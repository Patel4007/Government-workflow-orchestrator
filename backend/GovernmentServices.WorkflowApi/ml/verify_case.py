#!/usr/bin/env python3
import argparse
import json
import os
import re
import sys
from pathlib import Path
from typing import Any


def _fallback(case: dict[str, Any], model_id: str, reason: str) -> dict[str, Any]:
    documents = case.get("documents", [])
    findings = []
    verified = 0

    for document in documents:
        name = str(document.get("Name") or document.get("name") or "")
        doc_type = str(document.get("Type") or document.get("type") or "")

        looks_supported = "pdf" in doc_type.lower() or "image" in doc_type.lower()
        seems_government = any(
            token in name.lower()
            for token in ["tax", "benefit", "identity", "verification", "license", "w-2"]
        )

        if looks_supported and seems_government:
            verified += 1
            findings.append(f"{name} appears consistent with the requested service.")
        else:
            findings.append(f"{name or 'Unnamed document'} should be reviewed manually.")

    confidence = round(verified / len(documents), 2) if documents else 0.0
    return {
        "passed": confidence >= 0.75,
        "confidenceScore": confidence,
        "summary": reason,
        "model": f"{model_id} (python fallback)",
        "findings": findings or ["No documents were supplied for verification."],
    }


def _extract_json(text: str) -> dict[str, Any]:
    match = re.search(r"\{.*\}", text, re.DOTALL)
    if not match:
        raise ValueError("No JSON object found in model response.")
    return json.loads(match.group(0))


def _model_cache_exists(model_id: str) -> bool:
    huggingface_home = Path(os.environ.get("HF_HOME", Path.home() / ".cache" / "huggingface"))
    model_cache_dir = huggingface_home / "hub" / f"models--{model_id.replace('/', '--')}"
    return model_cache_dir.exists()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", required=True)
    args = parser.parse_args()

    payload_text = sys.stdin.read()
    case = json.loads(payload_text)

    use_local_model = os.environ.get("QWEN_ENABLE_LOCAL_MODEL", "0") == "1"

    if not use_local_model or not _model_cache_exists(args.model):
        result = _fallback(
            case,
            args.model,
            "The local Qwen model is not enabled or cached yet, so heuristic verification was used immediately.",
        )
        print(json.dumps(result))
        return 0

    try:
        from transformers import AutoModelForCausalLM, AutoTokenizer
        import torch
    except Exception:
        result = _fallback(case, args.model, "Transformers dependencies are unavailable, so heuristic verification was used.")
        print(json.dumps(result))
        return 0

    try:
        tokenizer = AutoTokenizer.from_pretrained(args.model, local_files_only=True)
        model = AutoModelForCausalLM.from_pretrained(
            args.model,
            torch_dtype="auto",
            device_map="auto",
            local_files_only=True,
        )
    except Exception:
        result = _fallback(case, args.model, "The Qwen model could not be loaded locally, so heuristic verification was used.")
        print(json.dumps(result))
        return 0

    prompt = f"""
You are a government workflow document verification bot.
Review the case payload and return ONLY valid JSON with this exact shape:
{{
  "passed": true,
  "confidenceScore": 0.0,
  "summary": "short summary",
  "model": "{args.model}",
  "findings": ["finding 1", "finding 2"]
}}

Rules:
- confidenceScore must be a number between 0 and 1.
- findings should be concise.
- If required evidence looks incomplete, set passed to false.

Case payload:
{json.dumps(case)}
""".strip()

    messages = [{"role": "user", "content": prompt}]
    input_text = tokenizer.apply_chat_template(messages, tokenize=False, add_generation_prompt=True)
    model_inputs = tokenizer([input_text], return_tensors="pt").to(model.device)
    generated_ids = model.generate(
        **model_inputs,
        max_new_tokens=220,
        temperature=0.1,
        do_sample=False,
    )
    generated_ids = [
        output_ids[len(input_ids):] for input_ids, output_ids in zip(model_inputs.input_ids, generated_ids)
    ]
    response = tokenizer.batch_decode(generated_ids, skip_special_tokens=True)[0]

    try:
        parsed = _extract_json(response)
    except Exception:
        parsed = _fallback(case, args.model, "The Qwen response was not valid JSON, so heuristic verification was used.")

    print(json.dumps(parsed))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
