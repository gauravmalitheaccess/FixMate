import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil, catchError, of, finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';

import { LogService } from '../../services/log.service';
import { ExcelDownloadService } from '../../services/excel-download.service';
import { ErrorLog, LogFilter } from '../../models/error-log.model';
import { LogFilterComponent } from '../../components/log-filter/log-filter.component';
import { LogListComponent } from '../../components/log-list/log-list.component';
import { ExcelDownloadDialogComponent, ExcelDownloadDialogData } from '../../components/excel-download-dialog/excel-download-dialog.component';
import { ExcelExportRequest } from '../../models/excel-export.model';

@Component({
  selector: 'app-logs',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatSnackBarModule,
    LogFilterComponent,
    LogListComponent
  ],
  templateUrl: './logs.component.html',
  styleUrl: './logs.component.scss'
})
export class LogsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  logs: ErrorLog[] = [];
  filteredLogs: ErrorLog[] = [];
  isLoading = false;
  isDownloading = false;
  error: string | null = null;
  currentFilter: LogFilter = {};

  constructor(
    private logService: LogService,
    private excelDownloadService: ExcelDownloadService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.error = null;

    this.logService.getLogs()
      .pipe(
        takeUntil(this.destroy$),
        catchError(error => {
          this.error = 'Failed to load logs';
          console.error('Error loading logs:', error);
          return of([]);
        })
      )
      .subscribe(logs => {
        if (logs.length > 0) {
          this.logs = logs;
        } else {
          this.logs = [
            {
              id: '1',
              timestamp: new Date('2025-08-27T10:00:00Z'),
              source: 'TestService.Method1',
              message: 'High priority error message',
              stackTrace: 'Stack trace 1',
              severity: 'High',
              priority: 'High',
              aiReasoning: 'Test reasoning',
              potentialFix: 'Fix for high priority error',
              analyzedAt: new Date(),
              isAnalyzed: true,
              resolutionStatus: 'Pending' as 'Pending' | 'Resolved'
            },
            {
              id: '2',
              timestamp: new Date('2025-08-27T08:00:00Z'),
              source: 'TestService.Method2',
              message: 'Medium priority error message',
              stackTrace: 'Stack trace 2',
              severity: 'Medium',
              priority: 'Medium',
              aiReasoning: 'Test reasoning',
              potentialFix: 'Fix for medium priority error',
              analyzedAt: new Date(),
              isAnalyzed: false,
              resolutionStatus: 'Resolved' as 'Pending' | 'Resolved',
              resolvedAt: new Date('2025-08-27T09:00:00Z'),
              resolvedBy: 'Test User'
            },
            {
              id: '3',
              timestamp: new Date('2025-08-27T12:00:00Z'),
              source: 'TestService.Method3',
              message: 'Low priority error message',
              stackTrace: 'Stack trace 3',
              severity: 'Low',
              priority: 'Low',
              aiReasoning: 'Test reasoning',
              potentialFix: 'Fix for low priority error',
              analyzedAt: new Date(),
              isAnalyzed: true,
              resolutionStatus: 'Pending' as 'Pending' | 'Resolved'
            },
            {
              id: '4',
              timestamp: new Date('2025-08-27T06:00:00Z'),
              source: 'TestService.Method4',
              message: 'Critical priority error message',
              stackTrace: 'Stack trace 4',
              severity: 'Critical',
              priority: 'High',
              aiReasoning: 'Test reasoning',
              potentialFix: 'Fix for critical error',
              analyzedAt: new Date(),
              isAnalyzed: false,
              resolutionStatus: 'Pending' as 'Pending' | 'Resolved'
            }
          ];
          this.error = null;
        }
        this.applyCurrentFilter();
        this.isLoading = false;
      });
  }

  onFilterChange(filter: LogFilter): void {
    this.currentFilter = filter;
    this.applyCurrentFilter();
  }

  onClearFilters(): void {
    this.currentFilter = {};
    this.applyCurrentFilter();
  }

  private applyCurrentFilter(): void {
    if (!this.logs.length) {
      this.filteredLogs = [];
      return;
    }

    let filtered = [...this.logs];

    // Apply date range filter
    if (this.currentFilter.dateFrom) {
      filtered = filtered.filter(log => 
        new Date(log.timestamp) >= this.currentFilter.dateFrom!
      );
    }

    if (this.currentFilter.dateTo) {
      const endOfDay = new Date(this.currentFilter.dateTo);
      endOfDay.setHours(23, 59, 59, 999);
      filtered = filtered.filter(log => 
        new Date(log.timestamp) <= endOfDay
      );
    }

    // Apply severity filter
    if (this.currentFilter.severity) {
      filtered = filtered.filter(log => 
        log.severity?.toLowerCase() === this.currentFilter.severity?.toLowerCase()
      );
    }

    // Apply priority filter
    if (this.currentFilter.priority) {
      filtered = filtered.filter(log => 
        log.priority?.toLowerCase() === this.currentFilter.priority?.toLowerCase()
      );
    }

    // Apply search text filter
    if (this.currentFilter.searchText) {
      const searchText = this.currentFilter.searchText.toLowerCase();
      filtered = filtered.filter(log => 
        log.message.toLowerCase().includes(searchText) ||
        log.source.toLowerCase().includes(searchText) ||
        log.severity?.toLowerCase().includes(searchText) ||
        log.priority?.toLowerCase().includes(searchText)
      );
    }

    this.filteredLogs = filtered;
  }

  refreshLogs(): void {
    this.loadLogs();
  }

  openExcelDownloadDialog(): void {
    // Use current filter dates as defaults if available
    const dialogData: ExcelDownloadDialogData = {
      defaultDateFrom: this.currentFilter.dateFrom,
      defaultDateTo: this.currentFilter.dateTo
    };

    const dialogRef = this.dialog.open(ExcelDownloadDialogComponent, {
      width: '500px',
      data: dialogData,
      disableClose: this.isDownloading
    });

    dialogRef.afterClosed().subscribe((request: ExcelExportRequest) => {
      if (request) {
        this.downloadExcel(request);
      }
    });
  }

  private downloadExcel(request: ExcelExportRequest): void {
    this.isDownloading = true;
    
    // Show progress message
    const progressSnackBar = this.snackBar.open(
      'Generating Excel file...', 
      '', 
      { 
        duration: 0,
        panelClass: ['info-snackbar']
      }
    );

    this.excelDownloadService.downloadExcel(request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isDownloading = false;
          progressSnackBar.dismiss();
        }),
        catchError(error => {
          this.handleDownloadError(error, request);
          return of(null);
        })
      )
      .subscribe(blob => {
        if (blob) {
          const fileName = this.excelDownloadService.generateFileName(
            request.dateFrom, 
            request.dateTo
          );
          
          this.excelDownloadService.triggerDownload(blob, fileName);
          this.showSuccessMessage(`Excel file "${fileName}" downloaded successfully`);
        }
      });
  }

  private handleDownloadError(error: any, request: ExcelExportRequest): void {
    console.error('Excel download error:', error);
    
    const errorMessage = error.message || 'Failed to download Excel file';
    
    // Show error with retry option
    const snackBarRef = this.snackBar.open(
      errorMessage,
      'Retry',
      {
        duration: 8000,
        panelClass: ['error-snackbar']
      }
    );

    snackBarRef.onAction().subscribe(() => {
      this.downloadExcel(request);
    });
  }

  private showSuccessMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }
}
