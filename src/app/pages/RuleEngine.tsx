import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { Switch } from "../components/ui/switch";
import { Badge } from "../components/ui/badge";
import { Input } from "../components/ui/input";
import { Textarea } from "../components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../components/ui/select";
import { Separator } from "../components/ui/separator";
import { Skeleton } from "../components/ui/skeleton";
import {
  Plus,
  Trash2,
  Play,
  Pause,
  GitBranch,
  Settings2,
  Edit,
  AlertCircle,
} from "lucide-react";
import { useApi } from "../hooks/useApi";
import { ruleService } from "../services/rule.service";
import type { RuleDto, RuleTestResultDto } from "../types/api.types";

function cloneRule(rule: RuleDto): RuleDto {
  return {
    ...rule,
    conditions: rule.conditions.map((condition) => ({ ...condition })),
    actions: rule.actions.map((action) => ({
      ...action,
      parameters: action.parameters ? { ...action.parameters } : undefined,
    })),
  };
}

function createEmptyRule(): RuleDto {
  const now = new Date().toISOString();
  return {
    id: "",
    name: "New Workflow Rule",
    description: "Describe when this rule should run.",
    enabled: true,
    priority: 5,
    conditions: [
      {
        id: `cond-${crypto.randomUUID()}`,
        field: "serviceType",
        operator: "equals",
        value: "document-verification",
      },
    ],
    actions: [
      {
        id: `act-${crypto.randomUUID()}`,
        type: "assign",
        target: "Operations Review",
      },
    ],
    createdAt: now,
    updatedAt: now,
  };
}

export function RuleEngine() {
  const [rules, setRules] = useState<RuleDto[]>([]);
  const [selectedRuleId, setSelectedRuleId] = useState<string | null>(null);
  const [draftRule, setDraftRule] = useState<RuleDto | null>(null);
  const [testResult, setTestResult] = useState<RuleTestResultDto | null>(null);

  const {
    data,
    loading,
    error,
    refetch,
  } = useApi(() => ruleService.getRules(), { immediate: false });

  useEffect(() => {
    refetch().catch(() => undefined);
  }, []);

  useEffect(() => {
    if (data?.data) {
      setRules(data.data);
      if (!selectedRuleId && data.data.length > 0) {
        setSelectedRuleId(data.data[0].id);
      }
    }
  }, [data?.data]);

  useEffect(() => {
    const selectedRule = rules.find((rule) => rule.id === selectedRuleId) || null;
    setDraftRule(selectedRule ? cloneRule(selectedRule) : null);
    setTestResult(null);
  }, [selectedRuleId, rules]);

  const selectedRuleData = rules.find((rule) => rule.id === selectedRuleId) || null;

  const replaceRule = (updatedRule: RuleDto) => {
    setRules((currentRules) =>
      currentRules.map((rule) => (rule.id === updatedRule.id ? updatedRule : rule))
    );
  };

  const handleToggleRule = async (ruleId: string) => {
    try {
      const response = await ruleService.toggleRule(ruleId);
      replaceRule(response.data);
      toast.success(response.message || "Rule state updated.");
    } catch (toggleError) {
      const message = toggleError instanceof Error ? toggleError.message : "Unable to toggle rule";
      toast.error(message);
    }
  };

  const handleCreateRule = async () => {
    try {
      const response = await ruleService.createRule(createEmptyRule());
      setRules((currentRules) => [response.data, ...currentRules]);
      setSelectedRuleId(response.data.id);
      toast.success("Rule created.");
    } catch (createError) {
      const message = createError instanceof Error ? createError.message : "Unable to create rule";
      toast.error(message);
    }
  };

  const handleSaveRule = async () => {
    if (!draftRule) {
      return;
    }

    try {
      const payload: RuleDto = {
        ...draftRule,
        conditions: draftRule.conditions.filter((condition) => condition.field && condition.operator),
        actions: draftRule.actions.filter((action) => action.type && action.target),
      };

      const response = draftRule.id
        ? await ruleService.updateRule(draftRule.id, payload)
        : await ruleService.createRule(payload);

      replaceRule(response.data);
      setSelectedRuleId(response.data.id);
      toast.success(response.message || "Rule saved.");
    } catch (saveError) {
      const message = saveError instanceof Error ? saveError.message : "Unable to save rule";
      toast.error(message);
    }
  };

  const handleDeleteRule = async (ruleId: string) => {
    if (!window.confirm("Delete this rule?")) {
      return;
    }

    try {
      await ruleService.deleteRule(ruleId);
      const remainingRules = rules.filter((rule) => rule.id !== ruleId);
      setRules(remainingRules);
      setSelectedRuleId(remainingRules[0]?.id ?? null);
      toast.success("Rule deleted.");
    } catch (deleteError) {
      const message = deleteError instanceof Error ? deleteError.message : "Unable to delete rule";
      toast.error(message);
    }
  };

  const handleTestRule = async (ruleId: string) => {
    try {
      const response = await ruleService.testRule(ruleId, {});
      setTestResult(response.data);
      toast.success(response.message || "Rule test completed.");
    } catch (testError) {
      const message = testError instanceof Error ? testError.message : "Unable to test rule";
      toast.error(message);
    }
  };

  const updateCondition = (conditionId: string, key: "field" | "operator" | "value", value: string) => {
    setDraftRule((currentRule) =>
      currentRule
        ? {
            ...currentRule,
            conditions: currentRule.conditions.map((condition) =>
              condition.id === conditionId ? { ...condition, [key]: value } : condition
            ),
          }
        : currentRule
    );
  };

  const updateAction = (actionId: string, key: "type" | "target", value: string) => {
    setDraftRule((currentRule) =>
      currentRule
        ? {
            ...currentRule,
            actions: currentRule.actions.map((action) =>
              action.id === actionId ? { ...action, [key]: value } : action
            ),
          }
        : currentRule
    );
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold text-gray-900">
            Rules Engine
          </h1>
          <p className="text-gray-500 mt-1">
            Configure automated workflow routing and decision rules
          </p>
        </div>
        <Button className="bg-blue-600 hover:bg-blue-700" onClick={handleCreateRule}>
          <Plus className="w-4 h-4 mr-2" />
          Create Rule
        </Button>
      </div>

      {error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 text-red-800">
            <AlertCircle className="w-5 h-5" />
            <p className="font-medium">Error loading rules</p>
          </div>
          <p className="text-sm text-red-600 mt-1">{error}</p>
          <Button onClick={() => refetch().catch(() => undefined)} variant="outline" size="sm" className="mt-3">
            Retry
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <Card className="border-gray-200 shadow-sm">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Active Rules</CardTitle>
                  <div className="flex items-center gap-2 text-sm text-gray-500">
                    <span>
                      {rules.filter((rule) => rule.enabled).length} of {rules.length} enabled
                    </span>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                {loading ? (
                  <div className="space-y-3">
                    {[...Array(4)].map((_, index) => (
                      <Skeleton key={index} className="h-40 w-full" />
                    ))}
                  </div>
                ) : (
                  <div className="space-y-3">
                    {rules.map((rule) => (
                      <div
                        key={rule.id}
                        className={`p-4 rounded-lg border-2 cursor-pointer transition-all hover:shadow-md ${
                          selectedRuleId === rule.id
                            ? "border-blue-600 bg-blue-50"
                            : "border-gray-200 bg-white"
                        }`}
                        onClick={() => setSelectedRuleId(rule.id)}
                      >
                        <div className="flex items-start justify-between mb-2">
                          <div className="flex items-center gap-3 flex-1">
                            <div
                              className={`w-10 h-10 rounded-lg flex items-center justify-center ${
                                rule.enabled ? "bg-green-100" : "bg-gray-100"
                              }`}
                            >
                              <GitBranch
                                className={`w-5 h-5 ${
                                  rule.enabled ? "text-green-600" : "text-gray-400"
                                }`}
                              />
                            </div>
                            <div className="flex-1">
                              <div className="flex items-center gap-2 mb-1">
                                <h3 className="font-semibold text-gray-900">
                                  {rule.name}
                                </h3>
                                <Badge
                                  variant="outline"
                                  className={
                                    rule.enabled
                                      ? "bg-green-100 text-green-800 border-green-300"
                                      : "bg-gray-100 text-gray-600 border-gray-300"
                                  }
                                >
                                  {rule.enabled ? "Active" : "Inactive"}
                                </Badge>
                              </div>
                              <p className="text-sm text-gray-600">
                                {rule.description}
                              </p>
                            </div>
                          </div>
                          <Switch
                            checked={rule.enabled}
                            onCheckedChange={() => handleToggleRule(rule.id)}
                            onClick={(event) => event.stopPropagation()}
                          />
                        </div>

                        <Separator className="my-3" />

                        <div className="grid grid-cols-2 gap-4 text-sm">
                          <div>
                            <p className="text-gray-500 mb-1">Conditions</p>
                            <p className="font-medium text-gray-900">
                              {rule.conditions.length} configured
                            </p>
                          </div>
                          <div>
                            <p className="text-gray-500 mb-1">Actions</p>
                            <p className="font-medium text-gray-900">
                              {rule.actions.length} configured
                            </p>
                          </div>
                        </div>

                        <div className="mt-3 flex gap-2">
                          <Button size="sm" variant="outline">
                            <Edit className="w-3 h-3 mr-1" />
                            Edit
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleToggleRule(rule.id);
                            }}
                          >
                            {rule.enabled ? (
                              <>
                                <Pause className="w-3 h-3 mr-1" />
                                Pause
                              </>
                            ) : (
                              <>
                                <Play className="w-3 h-3 mr-1" />
                                Activate
                              </>
                            )}
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            className="text-red-600 hover:bg-red-50"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleDeleteRule(rule.id);
                            }}
                          >
                            <Trash2 className="w-3 h-3 mr-1" />
                            Delete
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          <div className="lg:col-span-1">
            <Card className="border-gray-200 shadow-sm sticky top-6">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Rule Configuration</CardTitle>
                  <Settings2 className="w-5 h-5 text-gray-400" />
                </div>
              </CardHeader>
              <CardContent>
                {draftRule ? (
                  <div className="space-y-6">
                    <div>
                      <label className="text-sm font-medium text-gray-600">Rule Name</label>
                      <Input
                        className="mt-2"
                        value={draftRule.name}
                        onChange={(event) =>
                          setDraftRule({ ...draftRule, name: event.target.value })
                        }
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium text-gray-600">Description</label>
                      <Textarea
                        className="mt-2"
                        value={draftRule.description}
                        onChange={(event) =>
                          setDraftRule({ ...draftRule, description: event.target.value })
                        }
                      />
                    </div>

                    <div>
                      <label className="text-sm font-medium text-gray-600">Priority</label>
                      <Input
                        className="mt-2"
                        type="number"
                        min={1}
                        max={99}
                        value={draftRule.priority}
                        onChange={(event) =>
                          setDraftRule({
                            ...draftRule,
                            priority: Number(event.target.value || "1"),
                          })
                        }
                      />
                    </div>

                    <Separator />

                    <div>
                      <label className="text-sm font-semibold text-gray-900 mb-3 block">
                        Conditions (IF)
                      </label>
                      <div className="space-y-3">
                        {draftRule.conditions.map((condition, index) => (
                          <div key={condition.id} className="bg-blue-50 border border-blue-200 rounded-lg p-3">
                            {index > 0 && (
                              <div className="text-xs font-semibold text-blue-600 mb-2">
                                AND
                              </div>
                            )}
                            <div className="space-y-2">
                              <Input
                                value={condition.field}
                                className="text-sm"
                                placeholder="Field"
                                onChange={(event) => updateCondition(condition.id, "field", event.target.value)}
                              />
                              <Select
                                value={condition.operator}
                                onValueChange={(value) => updateCondition(condition.id, "operator", value)}
                              >
                                <SelectTrigger className="text-sm">
                                  <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                  <SelectItem value="equals">Equals</SelectItem>
                                  <SelectItem value="greaterThan">Greater Than</SelectItem>
                                  <SelectItem value="lessThan">Less Than</SelectItem>
                                </SelectContent>
                              </Select>
                              <Input
                                value={condition.value}
                                className="text-sm"
                                placeholder="Value"
                                onChange={(event) => updateCondition(condition.id, "value", event.target.value)}
                              />
                            </div>
                          </div>
                        ))}
                        <Button
                          size="sm"
                          variant="outline"
                          className="w-full"
                          onClick={() =>
                            setDraftRule({
                              ...draftRule,
                              conditions: [
                                ...draftRule.conditions,
                                {
                                  id: `cond-${crypto.randomUUID()}`,
                                  field: "",
                                  operator: "equals",
                                  value: "",
                                },
                              ],
                            })
                          }
                        >
                          <Plus className="w-3 h-3 mr-1" />
                          Add Condition
                        </Button>
                      </div>
                    </div>

                    <Separator />

                    <div>
                      <label className="text-sm font-semibold text-gray-900 mb-3 block">
                        Actions (THEN)
                      </label>
                      <div className="space-y-3">
                        {draftRule.actions.map((action) => (
                          <div key={action.id} className="bg-green-50 border border-green-200 rounded-lg p-3">
                            <div className="space-y-2">
                              <Select
                                value={action.type}
                                onValueChange={(value) => updateAction(action.id, "type", value)}
                              >
                                <SelectTrigger className="text-sm">
                                  <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                  <SelectItem value="assign">Assign</SelectItem>
                                  <SelectItem value="setPriority">Set Priority</SelectItem>
                                  <SelectItem value="skipStage">Skip Stage</SelectItem>
                                  <SelectItem value="approve">Approve</SelectItem>
                                  <SelectItem value="addFlag">Add Flag</SelectItem>
                                </SelectContent>
                              </Select>
                              <Input
                                value={action.target}
                                className="text-sm"
                                placeholder="Target"
                                onChange={(event) => updateAction(action.id, "target", event.target.value)}
                              />
                            </div>
                          </div>
                        ))}
                        <Button
                          size="sm"
                          variant="outline"
                          className="w-full"
                          onClick={() =>
                            setDraftRule({
                              ...draftRule,
                              actions: [
                                ...draftRule.actions,
                                {
                                  id: `act-${crypto.randomUUID()}`,
                                  type: "assign",
                                  target: "",
                                },
                              ],
                            })
                          }
                        >
                          <Plus className="w-3 h-3 mr-1" />
                          Add Action
                        </Button>
                      </div>
                    </div>

                    {testResult && (
                      <>
                        <Separator />
                        <div className="space-y-2">
                          <p className="text-sm font-semibold text-gray-900">Latest Test Result</p>
                          <p className="text-sm text-gray-600">{testResult.summary}</p>
                          <Badge
                            variant="outline"
                            className={
                              testResult.matched
                                ? "bg-green-100 text-green-800 border-green-300"
                                : "bg-amber-100 text-amber-800 border-amber-300"
                            }
                          >
                            {testResult.matched ? "Matched" : "Did Not Match"}
                          </Badge>
                          <div className="space-y-1 text-sm text-gray-600">
                            {testResult.executedActions.map((action, index) => (
                              <p key={index}>{action}</p>
                            ))}
                          </div>
                        </div>
                      </>
                    )}

                    <Separator />

                    <div className="flex gap-2">
                      <Button className="flex-1 bg-blue-600 hover:bg-blue-700" onClick={handleSaveRule}>
                        Save Changes
                      </Button>
                      {selectedRuleData && (
                        <Button variant="outline" onClick={() => handleTestRule(selectedRuleData.id)}>
                          Test Rule
                        </Button>
                      )}
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-12">
                    <GitBranch className="w-12 h-12 mx-auto mb-3 text-gray-300" />
                    <p className="text-sm text-gray-500">
                      Select a rule to view and edit its configuration
                    </p>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      )}
    </div>
  );
}
