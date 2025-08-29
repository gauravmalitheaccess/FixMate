import { Component, Input, OnInit, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';

import { ErrorLog } from '../../models/error-log.model';
import { PriorityIndicatorComponent } from '../priority-indicator/priority-indicator.component';
import { ExcelDownloadDialogComponent, ExcelDownloadDialogData } from '../excel-download-dialog/excel-download-dialog.component';
import { LogDetailsDialogComponent } from '../log-details-dialog/log-details-dialog.component';
import { StackTraceDialogComponent } from '../stack-trace-dialog/stack-trace-dialog.component';
import { ConfirmationDialogComponent } from '../confirmation-dialog/confirmation-dialog.component';
import { AdoStoryDialogComponent } from '../ado-story-dialog/ado-story-dialog.component';
import { ExcelDownloadService } from '../../services/excel-download.service';
import { LogService } from '../../services/log.service';
import { ExcelExportRequest } from '../../models/excel-export.model';

interface GroupedLog {
  pattern: string;
  logs: ErrorLog[];
  count: number;
  highestPriority: string;
  latestTimestamp: Date;
  isExpanded: boolean;
}

@Component({
  selector: 'app-log-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatSnackBarModule,
    MatMenuModule,
    MatDividerModule,
    PriorityIndicatorComponent,
    AdoStoryDialogComponent
  ],
  templateUrl: './log-list.component.html',
  styleUrl: './log-list.component.scss'
})
export class LogListComponent implements OnInit, OnChanges {
  @Input() logs: ErrorLog[] = [];
  @Input() isLoading: boolean = false;
  @Input() error: string | null = null;
  @Input() enableGrouping: boolean = false;

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  displayedColumns: string[] = [
    'priority',
    'timestamp', 
    'source', 
    'message', 
    'potentialFix',
    'severity', 
    'status',
    'actions'
  ];

  groupedDisplayedColumns: string[] = [
    'expand',
    'priority',
    'pattern',
    'count',
    'latestTimestamp',
    'actions'
  ];

  dataSource = new MatTableDataSource<ErrorLog>([]);
  groupedDataSource = new MatTableDataSource<GroupedLog>([]);
  groupedLogs: GroupedLog[] = [];
  isDownloadingExcel = false;

  constructor(
    private dialog: MatDialog,
    private excelDownloadService: ExcelDownloadService,
    private snackBar: MatSnackBar,
    private logService: LogService
  ) {}

  ngOnInit(): void {
    this.setupDataSource();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['logs']) {
      this.updateDataSource();
    }
  }

  private setupDataSource(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
    
    // Setup grouped data source
    this.groupedDataSource.paginator = this.paginator;
    this.groupedDataSource.sort = this.sort;
    
    // Custom sorting for priority
    this.dataSource.sortingDataAccessor = (data: ErrorLog, sortHeaderId: string) => {
      switch (sortHeaderId) {
        case 'priority':
          return this.getPrioritySortValue(data.priority);
        case 'severity':
          return this.getSeveritySortValue(data.severity);
        case 'timestamp':
          return new Date(data.timestamp).getTime();
        case 'status':
          return data.isAnalyzed ? 1 : 0;
        default:
          return (data as any)[sortHeaderId];
      }
    };

    // Custom sorting for grouped data
    this.groupedDataSource.sortingDataAccessor = (data: GroupedLog, sortHeaderId: string) => {
      switch (sortHeaderId) {
        case 'priority':
          return this.getPrioritySortValue(data.highestPriority);
        case 'count':
          return data.count;
        case 'latestTimestamp':
          return new Date(data.latestTimestamp).getTime();
        default:
          return (data as any)[sortHeaderId];
      }
    };

    // Custom filter predicate
    this.dataSource.filterPredicate = (data: ErrorLog, filter: string) => {
      const searchStr = filter.toLowerCase();
      return data.message.toLowerCase().includes(searchStr) ||
             data.source.toLowerCase().includes(searchStr) ||
             data.severity.toLowerCase().includes(searchStr) ||
             data.priority.toLowerCase().includes(searchStr) ||
             (data.potentialFix ? data.potentialFix.toLowerCase().includes(searchStr) : false);
    };

    this.updateDataSource();
  }

  private updateDataSource(): void {
    this.dataSource.data = [...this.logs];

    // Update grouped data if grouping is enabled
    if (this.enableGrouping) {
      this.updateGroupedDataSource();
    }
  }

  private updateGroupedDataSource(): void {
    const grouped = this.groupLogsByPattern(this.logs);
    
    this.groupedLogs = grouped;
    this.groupedDataSource.data = grouped;
  }

  private groupLogsByPattern(logs: ErrorLog[]): GroupedLog[] {
    const groups = new Map<string, ErrorLog[]>();
    
    logs.forEach(log => {
      const pattern = this.extractMessagePattern(log.message);
      if (!groups.has(pattern)) {
        groups.set(pattern, []);
      }
      groups.get(pattern)!.push(log);
    });

    return Array.from(groups.entries()).map(([pattern, groupLogs]) => {
      const sortedGroupLogs = groupLogs.sort((a, b) => 
        new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
      );
      
      const highestPriority = this.getHighestPriority(groupLogs);
      
      return {
        pattern,
        logs: sortedGroupLogs,
        count: groupLogs.length,
        highestPriority,
        latestTimestamp: sortedGroupLogs[0].timestamp,
        isExpanded: false
      };
    });
  }

  private extractMessagePattern(message: string): string {
    // Remove specific details like IDs, numbers, file paths to group similar errors
    return message
      .replace(/\bhttps?:\/\/[^\s]+/g, '[URL]') // URLs first, before paths
      .replace(/\b[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\b/gi, '[GUID]') // GUIDs before numbers
      .replace(/\b\w+@\w+\.\w+\b/g, '[EMAIL]') // Email addresses
      .replace(/\b[\w.-]+\.(js|ts|cs|java|py|php|rb)\b/gi, '[FILE]') // File names with extensions
      .replace(/\/[^\s]+/g, '[PATH]') // File paths
      .replace(/\b\d+\b/g, '[NUMBER]') // Numbers last
      .trim();
  }

  private getHighestPriority(logs: ErrorLog[]): string {
    const priorities = logs.map(log => log.priority?.toLowerCase() || 'unknown');
    
    if (priorities.includes('high')) return 'high';
    if (priorities.includes('medium')) return 'medium';
    if (priorities.includes('low')) return 'low';
    return 'unknown';
  }

  private getPrioritySortValue(priority: string): number {
    switch (priority?.toLowerCase()) {
      case 'high': return 3;
      case 'medium': return 2;
      case 'low': return 1;
      default: return 0;
    }
  }

  private getSeveritySortValue(severity: string): number {
    switch (severity?.toLowerCase()) {
      case 'critical': return 4;
      case 'high': return 3;
      case 'medium': return 2;
      case 'low': return 1;
      default: return 0;
    }
  }

  getSeverityColor(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'critical': return '#d32f2f';
      case 'high': return '#f44336';
      case 'medium': return '#ff9800';
      case 'low': return '#4caf50';
      default: return '#9e9e9e';
    }
  }

  getSeverityIcon(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'critical': return 'dangerous';
      case 'high': return 'warning';
      case 'medium': return 'info';
      case 'low': return 'check_circle';
      default: return 'help_outline';
    }
  }

  formatTimestamp(timestamp: Date): string {
    return new Date(timestamp).toLocaleString();
  }

  truncateMessage(message: string, maxLength: number = 100): string {
    if (message.length <= maxLength) {
      return message;
    }
    return message.substring(0, maxLength) + '...';
  }

  onViewDetails(log: ErrorLog): void {
    const dialogRef = this.dialog.open(LogDetailsDialogComponent, {
      width: '800px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      data: log,
      panelClass: 'log-details-dialog-panel'
    });

    dialogRef.afterClosed().subscribe(() => {
      // Dialog closed - no action needed
    });
  }

  onViewStackTrace(log: ErrorLog): void {
    if (!log.stackTrace) {
      this.showErrorMessage('No stack trace available for this log entry');
      return;
    }

    const dialogRef = this.dialog.open(StackTraceDialogComponent, {
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '80vh',
      data: log,
      panelClass: 'stack-trace-dialog-panel'
    });

    dialogRef.afterClosed().subscribe(() => {
      // Dialog closed - no action needed
    });
  }

  trackByLogId(index: number, log: ErrorLog): string {
    return log.id;
  }

  trackByGroupPattern(index: number, group: GroupedLog): string {
    return group.pattern;
  }

  toggleGroupExpansion(group: GroupedLog): void {
    group.isExpanded = !group.isExpanded;
  }

  toggleGroupingMode(): void {
    this.enableGrouping = !this.enableGrouping;
    this.updateDataSource();
  }

  applyFilter(filterValue: string): void {
    this.dataSource.filter = filterValue.trim().toLowerCase();
    
    if (this.enableGrouping) {
      this.groupedDataSource.filter = filterValue.trim().toLowerCase();
      this.groupedDataSource.filterPredicate = (data: GroupedLog, filter: string) => {
        return data.pattern.toLowerCase().includes(filter) ||
               data.logs.some(log => 
                 log.message.toLowerCase().includes(filter) ||
                 log.source.toLowerCase().includes(filter) ||
                 log.severity.toLowerCase().includes(filter) ||
                 log.priority.toLowerCase().includes(filter) ||
                 (log.potentialFix ? log.potentialFix.toLowerCase().includes(filter) : false)
               );
      };
    }
    
    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
    if (this.groupedDataSource.paginator) {
      this.groupedDataSource.paginator.firstPage();
    }
  }

  getTotalCount(): number {
    return this.logs.length;
  }

  getAnalyzedCount(): number {
    return this.logs.filter(log => log.resolutionStatus == "Resolved").length;
  }

  getUnanalyzedCount(): number {
    return this.logs.filter(log => log.resolutionStatus == "Pending").length;
  }

  getGroupedTotalCount(): number {
    return this.groupedLogs.length;
  }

  getExpandedGroupsCount(): number {
    return this.groupedLogs.filter(group => group.isExpanded).length;
  }

  onDownloadExcel(): void {
    const dialogData: ExcelDownloadDialogData = {
      defaultDateFrom: this.getEarliestLogDate(),
      defaultDateTo: this.getLatestLogDate()
    };

    const dialogRef = this.dialog.open(ExcelDownloadDialogComponent, {
      width: '600px',
      data: dialogData,
      disableClose: false
    });

    dialogRef.afterClosed().subscribe((request: ExcelExportRequest) => {
      if (request) {
        this.downloadExcelFile(request);
      }
    });
  }

  private downloadExcelFile(request: ExcelExportRequest): void {
    this.isDownloadingExcel = true;
    
    this.excelDownloadService.downloadExcel(request).subscribe({
      next: (blob: Blob) => {
        const fileName = this.excelDownloadService.generateFileName(request.dateFrom, request.dateTo);
        this.excelDownloadService.triggerDownload(blob, fileName);
        
        this.showSuccessMessage(`Excel file "${fileName}" downloaded successfully`);
        this.isDownloadingExcel = false;
      },
      error: (error: Error) => {
        this.showErrorMessage(error.message);
        this.isDownloadingExcel = false;
      }
    });
  }

  private getEarliestLogDate(): Date {
    if (this.logs.length === 0) {
      const weekAgo = new Date();
      weekAgo.setDate(weekAgo.getDate() - 7);
      return weekAgo;
    }

    const earliest = this.logs.reduce((min, log) => 
      new Date(log.timestamp) < new Date(min.timestamp) ? log : min
    );
    
    return new Date(earliest.timestamp);
  }

  private getLatestLogDate(): Date {
    if (this.logs.length === 0) {
      return new Date();
    }

    const latest = this.logs.reduce((max, log) => 
      new Date(log.timestamp) > new Date(max.timestamp) ? log : max
    );
    
    return new Date(latest.timestamp);
  }

  private showSuccessMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['success-snackbar']
    });
  }

  private showErrorMessage(message: string): void {
    this.snackBar.open(message, 'Retry', {
      duration: 8000,
      panelClass: ['error-snackbar']
    }).onAction().subscribe(() => {
      // User clicked retry - could reopen the dialog or retry last request
      this.onDownloadExcel();
    });
  }

  onChangePriority(log: ErrorLog): void {
    this.showSuccessMessage(`Change priority for log ${log.id} - Feature coming soon!`);
  }

  onCreateAdoStory(log: ErrorLog): void {
    if (!log.adoBug) {
      this.showErrorMessage('No ADO bug details available for this log entry');
      return;
    }

    const dialogRef = this.dialog.open(AdoStoryDialogComponent, {
      width: '800px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      data: log.adoBug,
      panelClass: 'ado-story-dialog-panel'
    });

    dialogRef.afterClosed().subscribe(() => {
      // Dialog closed - no action needed
    });
  }

  onIgnoreErrorType(log: ErrorLog): void {
    this.showSuccessMessage(`Ignore error type for log ${log.id} - Feature coming soon!`);
  }

  onMarkAsResolved(log: ErrorLog): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Mark as Resolved',
        message: 'Are you sure you want to mark this error log as resolved?',
        confirmText: 'Mark Resolved',
        cancelText: 'Cancel'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.logService.markLogAsResolved(log.id).subscribe({
          next: () => {
            // Update the log locally
            log.resolutionStatus = 'Resolved';
            log.resolvedAt = new Date();
            log.resolvedBy = 'Current User';
            
            this.snackBar.open('Log marked as resolved', 'Close', {
              duration: 3000,
              panelClass: ['success-snackbar']
            });
          },
          error: (error) => {
            console.error('Error marking log as resolved:', error);
            this.snackBar.open('Failed to mark log as resolved', 'Close', {
              duration: 3000,
              panelClass: ['error-snackbar']
            });
          }
        });
      }
    });
  }

}
