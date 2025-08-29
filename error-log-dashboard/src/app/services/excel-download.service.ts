import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map, retry } from 'rxjs/operators';
import { ExcelExportRequest, ExcelExportResponse } from '../models/excel-export.model';

@Injectable({
  providedIn: 'root'
})
export class ExcelDownloadService {
  private readonly baseUrl = 'https://localhost:44314/api/log';

  constructor(private http: HttpClient) {}

  /**
   * Downloads Excel file for the specified date range
   */
  downloadExcel(request: ExcelExportRequest): Observable<Blob> {
    const fromDateStr = request.dateFrom.toISOString().split('T')[0]; // Format as YYYY-MM-DD
    const toDateStr = request.dateTo.toISOString().split('T')[0]; // Format as YYYY-MM-DD
    
    const params = new HttpParams()
      .set('fromDate', fromDateStr)
      .set('toDate', toDateStr);

    return this.http.get(`${this.baseUrl}/export`, {
      params,
      responseType: 'blob',
      observe: 'response',
      headers: {
        'Accept': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      }
    }).pipe(
      retry(2), // Retry up to 2 times on failure
      map((response: HttpResponse<Blob>) => {
        if (!response.body) {
          throw new Error('No file content received');
        }
        return response.body;
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Downloads Excel file for a specific date
   */
  downloadExcelByDate(date: Date): Observable<Blob> {
    const dateStr = date.toISOString().split('T')[0]; // Format as YYYY-MM-DD
    
    return this.http.get(`${this.baseUrl}/export/${dateStr}`, {
      responseType: 'blob',
      observe: 'response',
      headers: {
        'Accept': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      }
    }).pipe(
      retry(2),
      map((response: HttpResponse<Blob>) => {
        if (!response.body) {
          throw new Error('No file content received');
        }
        return response.body;
      }),
      catchError(this.handleError)
    );
  }

  /**
   * Gets Excel export information without downloading
   */
  getExportInfo(request: ExcelExportRequest): Observable<ExcelExportResponse> {
    const fromDateStr = request.dateFrom.toISOString().split('T')[0]; // Format as YYYY-MM-DD
    const toDateStr = request.dateTo.toISOString().split('T')[0]; // Format as YYYY-MM-DD
    
    const params = new HttpParams()
      .set('fromDate', fromDateStr)
      .set('toDate', toDateStr);

    return this.http.get<ExcelExportResponse>(`${this.baseUrl}/export/info`, {
      params
    }).pipe(
      retry(2),
      catchError(this.handleError)
    );
  }

  /**
   * Triggers file download in the browser
   */
  triggerDownload(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.style.display = 'none';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    // Clean up the URL object
    window.URL.revokeObjectURL(url);
  }

  /**
   * Generates a filename based on date range
   */
  generateFileName(dateFrom: Date, dateTo: Date): string {
    const formatDate = (date: Date) => date.toISOString().split('T')[0];
    
    if (this.isSameDate(dateFrom, dateTo)) {
      return `error-logs-${formatDate(dateFrom)}.xlsx`;
    } else {
      return `error-logs-${formatDate(dateFrom)}-to-${formatDate(dateTo)}.xlsx`;
    }
  }

  private isSameDate(date1: Date, date2: Date): boolean {
    return date1.toDateString() === date2.toDateString();
  }

  private handleError(error: any): Observable<never> {
    let errorMessage = 'An error occurred while downloading the Excel file';
    
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = `Network error: ${error.error.message}`;
    } else {
      // Server-side error
      switch (error.status) {
        case 404:
          errorMessage = 'No data found for the selected date range';
          break;
        case 500:
          errorMessage = 'Server error occurred while generating the Excel file';
          break;
        case 0:
          errorMessage = 'Unable to connect to the server. Please check your connection.';
          break;
        default:
          errorMessage = `Server returned error code ${error.status}: ${error.message}`;
      }
    }
    
    return throwError(() => new Error(errorMessage));
  }
}