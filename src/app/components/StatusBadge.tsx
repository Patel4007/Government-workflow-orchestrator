import type { CaseStatus } from '../types';
import { getStatusColor } from '../utils/helpers';
import { Badge } from './ui/badge';

interface StatusBadgeProps {
  status: CaseStatus;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const colorClass = getStatusColor(status);
  
  return (
    <Badge 
      variant="outline" 
      className={`${colorClass} capitalize font-medium`}
    >
      {status.replace('-', ' ')}
    </Badge>
  );
}
