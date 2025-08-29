export interface ExcelExportRequest {
  dateFrom: Date;
  dateTo: Date;
  includeUnanalyzed?: boolean;
}

export interface ExcelExportResponse {
  fileName: string;
  downloadUrl: string;
  fileSize: number;
  generatedAt: Date;
}