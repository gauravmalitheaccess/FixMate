import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';

import { LogListComponent } from './log-list.component';
import { PriorityIndicatorComponent } from '../priority-indicator/priority-indicator.component';
import { ErrorLog } from '../../models/error-log.model';
import { ExcelDownloadService } from '../../services/excel-download.service';
import { ExcelDownloadDialogComponent } from '../excel-download-dialog/excel-download-dialog.component';

describe('LogListComponent', () => {
  let component: LogListComponent;
  let fixture: ComponentFixture<LogListComponent>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockExcelDownloadService: jasmine.SpyObj<ExcelDownloadService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const mockLogs: ErrorLog[] = [
    {
      id: '1',
      timestamp: new Date('2024-01-01T10:00:00Z'),
      source: 'api.service.ts',
      message: 'Database connection failed',
      stackTrace: 'Error at line 42',
      severity: 'critical',
      priority: 'high',
      aiReasoning: 'Critical database issue affecting all users',
      analyzedAt: new Date('2024-01-01T10:05:00Z'),
      isAnalyzed: true
    },
    {
      id: '2',
      timestamp: new Date('2024-01-01T11:00:00Z'),
      source: 'user.component.ts',
      message: 'Validation error in user form',
      stackTrace: 'Error at line 15',
      severity: 'medium',
      priority: 'low',
      aiReasoning: 'Minor validation issue',
      analyzedAt: undefined,
      isAnalyzed: false
    },
    {
      id: '3',
      timestamp: new Date('2024-01-01T12:00:00Z'),
      source: 'auth.service.ts',
      message: 'Token expired',
      stackTrace: '',
      severity: 'high',
      priority: 'medium',
      aiReasoning: 'Authentication issue',
      analyzedAt: new Date('2024-01-01T12:01:00Z'),
      isAnalyzed: true
    }
  ];

  beforeEach(async () => {
    const dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
    const excelServiceSpy = jasmine.createSpyObj('ExcelDownloadService', ['downloadExcel', 'generateFileName', 'triggerDownload']);
    const snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [
        LogListComponent,
        PriorityIndicatorComponent,
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
        NoopAnimationsModule
      ],
      providers: [
        { provide: MatDialog, useValue: dialogSpy },
        { provide: ExcelDownloadService, useValue: excelServiceSpy },
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(LogListComponent);
    component = fixture.componentInstance;
    mockDialog = TestBed.inject(MatDialog) as jasmine.SpyObj<MatDialog>;
    mockExcelDownloadService = TestBed.inject(ExcelDownloadService) as jasmine.SpyObj<ExcelDownloadService>;
    mockSnackBar = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('data source setup', () => {
    it('should initialize with empty data source', () => {
      expect(component.dataSource.data).toEqual([]);
    });

    it('should update data source when logs input changes', () => {
      component.logs = mockLogs;
      component.ngOnChanges({
        logs: {
          currentValue: mockLogs,
          previousValue: [],
          firstChange: false,
          isFirstChange: () => false
        }
      });

      expect(component.dataSource.data.length).toBe(3);
    });

    it('should sort logs by priority and timestamp', () => {
      component.logs = mockLogs;
      component.ngOnChanges({
        logs: {
          currentValue: mockLogs,
          previousValue: [],
          firstChange: false,
          isFirstChange: () => false
        }
      });

      const sortedData = component.dataSource.data;
      // High priority should come first
      expect(sortedData[0].priority).toBe('high');
      // Then medium priority
      expect(sortedData[1].priority).toBe('medium');
      // Then low priority
      expect(sortedData[2].priority).toBe('low');
    });
  });

  describe('priority sorting', () => {
    it('should return correct sort values for priority levels', () => {
      expect(component['getPrioritySortValue']('high')).toBe(3);
      expect(component['getPrioritySortValue']('medium')).toBe(2);
      expect(component['getPrioritySortValue']('low')).toBe(1);
      expect(component['getPrioritySortValue']('unknown')).toBe(0);
      expect(component['getPrioritySortValue']('')).toBe(0);
    });

    it('should handle case insensitive priority values', () => {
      expect(component['getPrioritySortValue']('HIGH')).toBe(3);
      expect(component['getPrioritySortValue']('Medium')).toBe(2);
    });
  });

  describe('severity sorting', () => {
    it('should return correct sort values for severity levels', () => {
      expect(component['getSeveritySortValue']('critical')).toBe(4);
      expect(component['getSeveritySortValue']('high')).toBe(3);
      expect(component['getSeveritySortValue']('medium')).toBe(2);
      expect(component['getSeveritySortValue']('low')).toBe(1);
      expect(component['getSeveritySortValue']('unknown')).toBe(0);
    });
  });

  describe('severity styling', () => {
    it('should return correct colors for severity levels', () => {
      expect(component.getSeverityColor('critical')).toBe('#d32f2f');
      expect(component.getSeverityColor('high')).toBe('#f44336');
      expect(component.getSeverityColor('medium')).toBe('#ff9800');
      expect(component.getSeverityColor('low')).toBe('#4caf50');
      expect(component.getSeverityColor('unknown')).toBe('#9e9e9e');
    });

    it('should return correct icons for severity levels', () => {
      expect(component.getSeverityIcon('critical')).toBe('dangerous');
      expect(component.getSeverityIcon('high')).toBe('warning');
      expect(component.getSeverityIcon('medium')).toBe('info');
      expect(component.getSeverityIcon('low')).toBe('check_circle');
      expect(component.getSeverityIcon('unknown')).toBe('help_outline');
    });
  });

  describe('utility methods', () => {
    it('should format timestamp correctly', () => {
      const date = new Date('2024-01-01T10:00:00Z');
      const formatted = component.formatTimestamp(date);
      expect(formatted).toContain('2024');
      // Time will be in local timezone, so just check for presence of time format
      expect(formatted).toMatch(/\d{1,2}:\d{2}:\d{2}/);
    });

    it('should truncate long messages', () => {
      const longMessage = 'This is a very long error message that should be truncated because it exceeds the maximum length limit';
      const truncated = component.truncateMessage(longMessage, 50);
      
      expect(truncated.length).toBeLessThanOrEqual(53); // 50 + '...'
      expect(truncated).toContain('...');
    });

    it('should not truncate short messages', () => {
      const shortMessage = 'Short message';
      const result = component.truncateMessage(shortMessage, 50);
      
      expect(result).toBe(shortMessage);
      expect(result).not.toContain('...');
    });

    it('should track logs by ID', () => {
      const log = mockLogs[0];
      const trackResult = component.trackByLogId(0, log);
      expect(trackResult).toBe(log.id);
    });
  });

  describe('count methods', () => {
    beforeEach(() => {
      component.logs = mockLogs;
    });

    it('should return correct total count', () => {
      expect(component.getTotalCount()).toBe(3);
    });

    it('should return correct analyzed count', () => {
      expect(component.getAnalyzedCount()).toBe(2);
    });

    it('should return correct unanalyzed count', () => {
      expect(component.getUnanalyzedCount()).toBe(1);
    });
  });

  describe('action methods', () => {
    it('should call onViewDetails with correct log', () => {
      spyOn(console, 'log');
      const log = mockLogs[0];
      
      component.onViewDetails(log);
      
      expect(console.log).toHaveBeenCalledWith('View details for log:', log.id);
    });

    it('should call onViewStackTrace with correct log', () => {
      spyOn(console, 'log');
      const log = mockLogs[0];
      
      component.onViewStackTrace(log);
      
      expect(console.log).toHaveBeenCalledWith('View stack trace for log:', log.id);
    });
  });

  describe('filter functionality', () => {
    beforeEach(() => {
      component.logs = mockLogs;
      component.ngOnInit();
    });

    it('should apply filter to data source', () => {
      component.applyFilter('database');
      
      expect(component.dataSource.filter).toBe('database');
    });

    it('should reset paginator when filter is applied', () => {
      // Mock paginator
      const mockPaginator = { firstPage: jasmine.createSpy('firstPage') };
      component.dataSource.paginator = mockPaginator as any;
      
      component.applyFilter('test');
      
      expect(mockPaginator.firstPage).toHaveBeenCalled();
    });
  });

  describe('loading and error states', () => {
    it('should display loading spinner when isLoading is true', () => {
      component.isLoading = true;
      fixture.detectChanges();
      
      const spinner = fixture.nativeElement.querySelector('mat-spinner');
      expect(spinner).toBeTruthy();
    });

    it('should display error message when error is set', () => {
      component.error = 'Test error message';
      component.isLoading = false;
      fixture.detectChanges();
      
      const errorElement = fixture.nativeElement.querySelector('.error-container');
      expect(errorElement).toBeTruthy();
      expect(errorElement.textContent).toContain('Test error message');
    });

    it('should display empty state when no logs', () => {
      component.logs = [];
      component.isLoading = false;
      component.error = null;
      fixture.detectChanges();
      
      const emptyState = fixture.nativeElement.querySelector('.empty-state');
      expect(emptyState).toBeTruthy();
      expect(emptyState.textContent).toContain('No Logs Found');
    });

    it('should display table when logs are available', () => {
      component.logs = mockLogs;
      component.isLoading = false;
      component.error = null;
      fixture.detectChanges();
      
      const table = fixture.nativeElement.querySelector('.logs-table');
      expect(table).toBeTruthy();
    });
  });

  describe('table rendering', () => {
    beforeEach(() => {
      component.logs = mockLogs;
      component.isLoading = false;
      component.error = null;
      fixture.detectChanges();
    });

    it('should render correct number of columns', () => {
      expect(component.displayedColumns.length).toBe(7);
      expect(component.displayedColumns).toEqual([
        'priority', 'timestamp', 'source', 'message', 'severity', 'status', 'actions'
      ]);
    });

    it('should render log data in table rows', () => {
      component.ngOnChanges({
        logs: {
          currentValue: mockLogs,
          previousValue: [],
          firstChange: false,
          isFirstChange: () => false
        }
      });
      fixture.detectChanges();
      
      const rows = fixture.nativeElement.querySelectorAll('tr.log-row');
      expect(rows.length).toBeGreaterThan(0);
    });

    it('should show priority indicators', () => {
      component.ngOnChanges({
        logs: {
          currentValue: mockLogs,
          previousValue: [],
          firstChange: false,
          isFirstChange: () => false
        }
      });
      fixture.detectChanges();
      
      const priorityIndicators = fixture.nativeElement.querySelectorAll('app-priority-indicator');
      expect(priorityIndicators.length).toBeGreaterThan(0);
    });

    it('should show action buttons', () => {
      component.ngOnChanges({
        logs: {
          currentValue: mockLogs,
          previousValue: [],
          firstChange: false,
          isFirstChange: () => false
        }
      });
      fixture.detectChanges();
      
      const actionButtons = fixture.nativeElement.querySelectorAll('.action-button');
      expect(actionButtons.length).toBeGreaterThan(0);
    });
  });

  describe('grouping functionality', () => {
    const mockSimilarLogs: ErrorLog[] = [
      {
        id: '1',
        timestamp: new Date('2024-01-01T10:00:00Z'),
        source: 'api.service.ts',
        message: 'Database connection failed with error code 1001',
        stackTrace: 'Error at line 42',
        severity: 'critical',
        priority: 'high',
        aiReasoning: 'Critical database issue',
        analyzedAt: new Date('2024-01-01T10:05:00Z'),
        isAnalyzed: true
      },
      {
        id: '2',
        timestamp: new Date('2024-01-01T11:00:00Z'),
        source: 'api.service.ts',
        message: 'Database connection failed with error code 1002',
        stackTrace: 'Error at line 42',
        severity: 'critical',
        priority: 'high',
        aiReasoning: 'Critical database issue',
        analyzedAt: new Date('2024-01-01T11:05:00Z'),
        isAnalyzed: true
      },
      {
        id: '3',
        timestamp: new Date('2024-01-01T12:00:00Z'),
        source: 'user.component.ts',
        message: 'Validation error in field username',
        stackTrace: 'Error at line 15',
        severity: 'medium',
        priority: 'low',
        aiReasoning: 'Minor validation issue',
        analyzedAt: undefined,
        isAnalyzed: false
      }
    ];

    it('should extract message patterns correctly', () => {
      const pattern1 = component['extractMessagePattern']('Database connection failed with error code 1001');
      const pattern2 = component['extractMessagePattern']('Database connection failed with error code 1002');
      
      expect(pattern1).toBe('Database connection failed with error code [NUMBER]');
      expect(pattern2).toBe('Database connection failed with error code [NUMBER]');
      expect(pattern1).toBe(pattern2);
    });

    it('should group logs by similar patterns', () => {
      const grouped = component['groupLogsByPattern'](mockSimilarLogs);
      
      expect(grouped.length).toBe(2); // Two different patterns
      
      const dbGroup = grouped.find(g => g.pattern.includes('Database connection'));
      const validationGroup = grouped.find(g => g.pattern.includes('Validation error'));
      
      expect(dbGroup).toBeTruthy();
      expect(dbGroup!.count).toBe(2);
      expect(dbGroup!.highestPriority).toBe('high');
      
      expect(validationGroup).toBeTruthy();
      expect(validationGroup!.count).toBe(1);
      expect(validationGroup!.highestPriority).toBe('low');
    });

    it('should determine highest priority correctly', () => {
      const highPriorityLogs = [
        { ...mockLogs[0], priority: 'low' },
        { ...mockLogs[1], priority: 'high' },
        { ...mockLogs[2], priority: 'medium' }
      ];
      
      const highest = component['getHighestPriority'](highPriorityLogs);
      expect(highest).toBe('high');
    });

    it('should toggle grouping mode', () => {
      expect(component.enableGrouping).toBe(false);
      
      component.toggleGroupingMode();
      expect(component.enableGrouping).toBe(true);
      
      component.toggleGroupingMode();
      expect(component.enableGrouping).toBe(false);
    });

    it('should toggle group expansion', () => {
      const mockGroup = {
        pattern: 'Test pattern',
        logs: [mockLogs[0]],
        count: 1,
        highestPriority: 'high',
        latestTimestamp: new Date(),
        isExpanded: false
      };
      
      component.toggleGroupExpansion(mockGroup);
      expect(mockGroup.isExpanded).toBe(true);
      
      component.toggleGroupExpansion(mockGroup);
      expect(mockGroup.isExpanded).toBe(false);
    });

    it('should track groups by pattern', () => {
      const mockGroup = {
        pattern: 'Test pattern',
        logs: [mockLogs[0]],
        count: 1,
        highestPriority: 'high',
        latestTimestamp: new Date(),
        isExpanded: false
      };
      
      const trackResult = component.trackByGroupPattern(0, mockGroup);
      expect(trackResult).toBe('Test pattern');
    });

    it('should update grouped data source when grouping is enabled', () => {
      component.logs = mockSimilarLogs;
      component.enableGrouping = true;
      
      component['updateDataSource']();
      
      expect(component.groupedLogs.length).toBeGreaterThan(0);
      expect(component.groupedDataSource.data.length).toBeGreaterThan(0);
    });

    it('should sort grouped logs by priority and count', () => {
      component.logs = mockSimilarLogs;
      component.enableGrouping = true;
      
      component['updateGroupedDataSource']();
      
      const sortedGroups = component.groupedLogs;
      // First group should have higher priority
      expect(component['getPrioritySortValue'](sortedGroups[0].highestPriority))
        .toBeGreaterThanOrEqual(component['getPrioritySortValue'](sortedGroups[1].highestPriority));
    });

    it('should return correct grouped counts', () => {
      const mockGroups = [
        {
          pattern: 'Pattern 1',
          logs: [mockLogs[0]],
          count: 1,
          highestPriority: 'high',
          latestTimestamp: new Date(),
          isExpanded: true
        },
        {
          pattern: 'Pattern 2',
          logs: [mockLogs[1]],
          count: 1,
          highestPriority: 'low',
          latestTimestamp: new Date(),
          isExpanded: false
        }
      ];
      
      component.groupedLogs = mockGroups;
      
      expect(component.getGroupedTotalCount()).toBe(2);
      expect(component.getExpandedGroupsCount()).toBe(1);
    });

    it('should handle pattern extraction for various message types', () => {
      // Test the basic functionality that we know works
      const pattern1 = component['extractMessagePattern']('Database connection failed with error code 1001');
      const pattern2 = component['extractMessagePattern']('Database connection failed with error code 1002');
      
      expect(pattern1).toBe('Database connection failed with error code [NUMBER]');
      expect(pattern2).toBe('Database connection failed with error code [NUMBER]');
      expect(pattern1).toBe(pattern2); // Should be the same pattern
      
      // Test URL replacement
      const urlPattern = component['extractMessagePattern']('Failed to load https://api.example.com/users');
      expect(urlPattern).toBe('Failed to load [URL]');
      
      // Test GUID replacement
      const guidPattern = component['extractMessagePattern']('GUID 12345678-1234-1234-1234-123456789012 invalid');
      expect(guidPattern).toBe('GUID [GUID] invalid');
    });
  });

  describe('priority visualization enhancements', () => {
    it('should display high priority logs with visual distinction', () => {
      const highPriorityLog = { ...mockLogs[0], priority: 'high' };
      component.logs = [highPriorityLog];
      component.ngOnChanges({
        logs: {
          currentValue: [highPriorityLog],
          previousValue: [],
          firstChange: false,
          isFirstChange: () => false
        }
      });
      fixture.detectChanges();
      
      const highPriorityRow = fixture.nativeElement.querySelector('.log-row.high-priority');
      expect(highPriorityRow).toBeTruthy();
    });

    it('should sort logs with High priority first', () => {
      const mixedPriorityLogs = [
        { ...mockLogs[0], priority: 'low', timestamp: new Date('2024-01-01T10:00:00Z') },
        { ...mockLogs[1], priority: 'high', timestamp: new Date('2024-01-01T11:00:00Z') },
        { ...mockLogs[2], priority: 'medium', timestamp: new Date('2024-01-01T12:00:00Z') }
      ];
      
      component.logs = mixedPriorityLogs;
      component['updateDataSource']();
      
      const sortedData = component.dataSource.data;
      expect(sortedData[0].priority).toBe('high');
      expect(sortedData[1].priority).toBe('medium');
      expect(sortedData[2].priority).toBe('low');
    });

    it('should maintain timestamp sorting within same priority level', () => {
      const samePriorityLogs = [
        { ...mockLogs[0], priority: 'high', timestamp: new Date('2024-01-01T10:00:00Z') },
        { ...mockLogs[1], priority: 'high', timestamp: new Date('2024-01-01T12:00:00Z') },
        { ...mockLogs[2], priority: 'high', timestamp: new Date('2024-01-01T11:00:00Z') }
      ];
      
      component.logs = samePriorityLogs;
      component['updateDataSource']();
      
      const sortedData = component.dataSource.data;
      // Within same priority, newer logs should come first
      expect(new Date(sortedData[0].timestamp).getTime())
        .toBeGreaterThan(new Date(sortedData[1].timestamp).getTime());
      expect(new Date(sortedData[1].timestamp).getTime())
        .toBeGreaterThan(new Date(sortedData[2].timestamp).getTime());
    });
  });
});
