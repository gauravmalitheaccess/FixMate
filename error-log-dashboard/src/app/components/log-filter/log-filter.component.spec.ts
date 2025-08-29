import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { LogFilterComponent } from './log-filter.component';
import { LogFilter } from '../../models/error-log.model';

describe('LogFilterComponent', () => {
  let component: LogFilterComponent;
  let fixture: ComponentFixture<LogFilterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        LogFilterComponent,
        ReactiveFormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatDatepickerModule,
        MatNativeDateModule,
        MatButtonModule,
        MatIconModule,
        MatChipsModule,
        NoopAnimationsModule
      ]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(LogFilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('form initialization', () => {
    it('should initialize form with empty values', () => {
      expect(component.filterForm.get('dateFrom')?.value).toBeNull();
      expect(component.filterForm.get('dateTo')?.value).toBeNull();
      expect(component.filterForm.get('severity')?.value).toBe('');
      expect(component.filterForm.get('priority')?.value).toBe('');
      expect(component.filterForm.get('searchText')?.value).toBe('');
    });

    it('should have correct severity options', () => {
      expect(component.severityOptions).toEqual([
        { value: 'critical', label: 'Critical' },
        { value: 'high', label: 'High' },
        { value: 'medium', label: 'Medium' },
        { value: 'low', label: 'Low' }
      ]);
    });

    it('should have correct priority options', () => {
      expect(component.priorityOptions).toEqual([
        { value: 'high', label: 'High Priority' },
        { value: 'medium', label: 'Medium Priority' },
        { value: 'low', label: 'Low Priority' }
      ]);
    });
  });

  describe('filter change emission', () => {
    it('should emit filter change when form values change', fakeAsync(() => {
      let emittedFilter: LogFilter | undefined;
      component.filterChange.subscribe(filter => emittedFilter = filter);

      component.filterForm.patchValue({
        severity: 'high',
        priority: 'medium'
      });

      tick(300); // Wait for debounce

      expect(emittedFilter).toEqual(jasmine.objectContaining({
        severity: 'high',
        priority: 'medium'
      }));
    }));

    it('should debounce search text changes', fakeAsync(() => {
      let emissionCount = 0;
      component.filterChange.subscribe(() => emissionCount++);

      // Rapid changes
      component.filterForm.patchValue({ searchText: 'e' });
      tick(100);
      component.filterForm.patchValue({ searchText: 'er' });
      tick(100);
      component.filterForm.patchValue({ searchText: 'err' });
      tick(100);
      component.filterForm.patchValue({ searchText: 'error' });
      
      // Should not emit yet
      expect(emissionCount).toBe(0);
      
      tick(300); // Complete debounce
      expect(emissionCount).toBe(1);
    }));

    it('should emit empty filter when all values are cleared', fakeAsync(() => {
      let emittedFilter: LogFilter | undefined;
      component.filterChange.subscribe(filter => emittedFilter = filter);

      // Set some values first
      component.filterForm.patchValue({
        severity: 'high',
        searchText: 'test'
      });
      tick(300);

      // Clear all values
      component.filterForm.patchValue({
        severity: '',
        searchText: ''
      });
      tick(300);

      expect(emittedFilter).toEqual({});
    }));

    it('should handle date filters correctly', fakeAsync(() => {
      let emittedFilter: LogFilter | undefined;
      component.filterChange.subscribe(filter => emittedFilter = filter);

      const fromDate = new Date('2024-01-01');
      const toDate = new Date('2024-01-31');

      component.filterForm.patchValue({
        dateFrom: fromDate,
        dateTo: toDate
      });

      tick(300);

      expect(emittedFilter?.dateFrom).toEqual(fromDate);
      expect(emittedFilter?.dateTo).toEqual(toDate);
    }));
  });

  describe('clear filters functionality', () => {
    it('should clear all form values when onClearFilters is called', () => {
      // Set some values
      component.filterForm.patchValue({
        dateFrom: new Date(),
        dateTo: new Date(),
        severity: 'high',
        priority: 'medium',
        searchText: 'test'
      });

      component.onClearFilters();

      expect(component.filterForm.get('dateFrom')?.value).toBeNull();
      expect(component.filterForm.get('dateTo')?.value).toBeNull();
      expect(component.filterForm.get('severity')?.value).toBe('');
      expect(component.filterForm.get('priority')?.value).toBe('');
      expect(component.filterForm.get('searchText')?.value).toBe('');
    });

    it('should emit clearFilters event when onClearFilters is called', () => {
      spyOn(component.clearFilters, 'emit');
      spyOn(component.filterChange, 'emit');

      component.onClearFilters();

      expect(component.clearFilters.emit).toHaveBeenCalled();
      expect(component.filterChange.emit).toHaveBeenCalledWith({});
    });
  });

  describe('active filters detection', () => {
    it('should return false when no filters are active', () => {
      expect(component.hasActiveFilters()).toBeFalsy();
    });

    it('should return true when any filter has a value', () => {
      component.filterForm.patchValue({ severity: 'high' });
      expect(component.hasActiveFilters()).toBeTruthy();

      component.filterForm.patchValue({ severity: '', searchText: 'test' });
      expect(component.hasActiveFilters()).toBeTruthy();
    });

    it('should return correct active filter count', () => {
      expect(component.getActiveFilterCount()).toBe(0);

      component.filterForm.patchValue({
        severity: 'high',
        priority: 'medium'
      });
      expect(component.getActiveFilterCount()).toBe(2);

      component.filterForm.patchValue({
        severity: 'high',
        priority: 'medium',
        searchText: 'error',
        dateFrom: new Date()
      });
      expect(component.getActiveFilterCount()).toBe(4);
    });
  });

  describe('date validation', () => {
    it('should clear dateTo when dateFrom is after dateTo', () => {
      const laterDate = new Date('2024-02-01');
      const earlierDate = new Date('2024-01-01');

      component.filterForm.patchValue({
        dateFrom: earlierDate,
        dateTo: laterDate
      });

      // Set dateFrom to a date after dateTo
      component.filterForm.patchValue({ dateFrom: new Date('2024-03-01') });
      component.onDateFromChange();

      expect(component.filterForm.get('dateTo')?.value).toBeNull();
    });

    it('should clear dateFrom when dateTo is before dateFrom', () => {
      const laterDate = new Date('2024-02-01');
      const earlierDate = new Date('2024-01-01');

      component.filterForm.patchValue({
        dateFrom: laterDate,
        dateTo: laterDate
      });

      // Set dateTo to a date before dateFrom
      component.filterForm.patchValue({ dateTo: earlierDate });
      component.onDateToChange();

      expect(component.filterForm.get('dateFrom')?.value).toBeNull();
    });

    it('should not clear dates when they are in correct order', () => {
      const fromDate = new Date('2024-01-01');
      const toDate = new Date('2024-01-31');

      component.filterForm.patchValue({
        dateFrom: fromDate,
        dateTo: toDate
      });

      component.onDateFromChange();
      component.onDateToChange();

      expect(component.filterForm.get('dateFrom')?.value).toEqual(fromDate);
      expect(component.filterForm.get('dateTo')?.value).toEqual(toDate);
    });
  });

  describe('component cleanup', () => {
    it('should complete destroy subject on ngOnDestroy', () => {
      spyOn(component['destroy$'], 'next');
      spyOn(component['destroy$'], 'complete');

      component.ngOnDestroy();

      expect(component['destroy$'].next).toHaveBeenCalled();
      expect(component['destroy$'].complete).toHaveBeenCalled();
    });
  });
});
