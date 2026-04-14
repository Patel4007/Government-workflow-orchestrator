import { useState, useEffect } from 'react';

export interface UseApiOptions {
  immediate?: boolean;
}

export interface UseApiState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

/**
 * Custom hook for API calls with loading and error states
 * Perfect for integrating with .NET Core API endpoints
 * 
 * @example
 * const { data, loading, error, execute } = useApi(
 *   () => caseService.getCases(),
 *   { immediate: true }
 * );
 */
export function useApi<T>(
  apiCall: () => Promise<T>,
  options: UseApiOptions = {}
) {
  const { immediate = false } = options;

  const [state, setState] = useState<UseApiState<T>>({
    data: null,
    loading: immediate,
    error: null,
  });

  const execute = async () => {
    setState({ data: null, loading: true, error: null });

    try {
      const result = await apiCall();
      setState({ data: result, loading: false, error: null });
      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setState({ data: null, loading: false, error: errorMessage });
      throw err;
    }
  };

  useEffect(() => {
    if (immediate) {
      execute();
    }
  }, []);

  return {
    ...state,
    execute,
    refetch: execute,
  };
}

/**
 * Hook for handling mutations (POST, PUT, DELETE)
 * 
 * @example
 * const { mutate, loading, error } = useMutation(
 *   (id: string) => caseService.approveCase(id)
 * );
 */
export function useMutation<TData, TVariables = void>(
  mutationFn: (variables: TVariables) => Promise<TData>
) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = async (variables: TVariables) => {
    setLoading(true);
    setError(null);

    try {
      const result = await mutationFn(variables);
      setLoading(false);
      return result;
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
      setLoading(false);
      throw err;
    }
  };

  return {
    mutate,
    loading,
    error,
    isLoading: loading,
    isError: error !== null,
  };
}

/**
 * Hook for paginated API calls
 * 
 * @example
 * const { data, loading, page, setPage, totalPages } = usePagination(
 *   (pageNum) => caseService.getCases({ pageNumber: pageNum })
 * );
 */
export function usePagination<T>(
  apiCall: (page: number, pageSize: number) => Promise<{ items: T[]; totalPages: number }>,
  initialPageSize = 10
) {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(initialPageSize);
  const [data, setData] = useState<T[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPage = async (pageNum: number) => {
    setLoading(true);
    setError(null);

    try {
      const result = await apiCall(pageNum, pageSize);
      setData(result.items);
      setTotalPages(result.totalPages);
      setLoading(false);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMessage);
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPage(page);
  }, [page, pageSize]);

  return {
    data,
    loading,
    error,
    page,
    setPage,
    pageSize,
    totalPages,
    refetch: () => fetchPage(page),
  };
}
