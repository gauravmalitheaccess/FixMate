export interface ErrorLog {
  id: string;
  timestamp: Date;
  source: string;
  message: string;
  stackTrace: string;
  severity: string;
  priority: string;
  aiReasoning: string;
  potentialFix: string;
  analyzedAt?: Date;
  isAnalyzed: boolean;
  resolutionStatus: 'Pending' | 'Resolved';
  resolvedAt?: Date;
  resolvedBy?: string;
  adoBug?: AdoBug;
}

export interface AdoBug {
  title: string;
  description: string;
  reproSteps: string;
  severity: string;
  priority: string;
  areaPath: string;
  assignedTo: string;
  tags: string[];
}

export interface LogFilter {
  dateFrom?: Date;
  dateTo?: Date;
  severity?: string;
  priority?: string;
  searchText?: string;
}

export interface LogStatistics {
  totalLogs: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  analyzedCount: number;
  unanalyzedCount: number;
  analyzedLogs: number;
  unanalyzedLogs: number;
  highPriorityCount: number;
  mediumPriorityCount: number;
  lowPriorityCount: number;
  todayCount: number;
  weekCount: number;
  monthCount: number;
  generatedAt: Date;
  lastUpdated: Date;
  severityBreakdown: { [key: string]: number };
  priorityBreakdown: { [key: string]: number };
  dateRange: {
    fromDate: Date;
    toDate: Date;
  };
}
