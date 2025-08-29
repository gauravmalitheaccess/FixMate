import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ErrorLog, LogFilter, LogStatistics } from '../models/error-log.model';
import { ExcelExportRequest } from '../models/excel-export.model';

@Injectable({
  providedIn: 'root'
})
export class LogService {
  private readonly apiUrl = 'https://localhost:44314/api';

  constructor(private http: HttpClient) { }

  getLogs(filter?: LogFilter): Observable<ErrorLog[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.dateFrom) {
        params = params.set('fromDate', filter.dateFrom.toISOString());
      }
      if (filter.dateTo) {
        params = params.set('toDate', filter.dateTo.toISOString());
      }
      if (filter.severity) {
        params = params.set('severity', filter.severity);
      }
      if (filter.priority) {
        params = params.set('priority', filter.priority);
      }
      if (filter.searchText) {
        params = params.set('searchText', filter.searchText);
      }
    }

    return this.http.get<any>(`${this.apiUrl}/log`, { params }).pipe(
      map(response => response.logs || [])
    );
  }

  getLogStatistics(): Observable<LogStatistics> {
    return this.http.get<LogStatistics>(`${this.apiUrl}/log/stats`);
  }

  generateSampleLogs(count: number = 10, severity?: string, hoursBack: number = 24): Observable<any> {
    let params = new HttpParams()
      .set('count', count.toString())
      .set('hoursBack', hoursBack.toString());
    
    if (severity) {
      params = params.set('severity', severity);
    }

    return this.http.post(`${this.apiUrl}/log/generate`, null, { params });
  }

  exportToExcel(request: ExcelExportRequest): Observable<Blob> {
    const dateStr = request.dateFrom.toISOString().split('T')[0];
    return this.http.get(`${this.apiUrl}/log/export/${dateStr}`, {
      responseType: 'blob'
    });
  }

  collectLogs(logs: ErrorLog[]): Observable<any> {
    return this.http.post(`${this.apiUrl}/log/collect`, logs);
  }

  analyzeRawExceptions(rawExceptions: any[]): Observable<ErrorLog[]> {
    return this.http.post<ErrorLog[]>(`${this.apiUrl}/log/analyze`, rawExceptions);
  }

  markLogAsResolved(logId: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/log/${logId}/resolve`, {
      resolutionStatus: 'Resolved',
      resolvedAt: new Date(),
      resolvedBy: 'Current User' // This would come from auth service in real implementation
    });
  }


 
}
