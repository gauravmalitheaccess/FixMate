import { Component, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';

import { LogFilter } from '../../models/error-log.model';

@Component({
  selector: 'app-log-filter',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule
  ],
  templateUrl: './log-filter.component.html',
  styleUrl: './log-filter.component.scss'
})
export class LogFilterComponent implements OnInit, OnDestroy {
  @Output() filterChange = new EventEmitter<LogFilter>();
  @Output() clearFilters = new EventEmitter<void>();

  private destroy$ = new Subject<void>();
  
  filterForm: FormGroup;
  
  severityOptions = [
    { value: 'critical', label: 'Critical' },
    { value: 'high', label: 'High' },
    { value: 'medium', label: 'Medium' },
    { value: 'low', label: 'Low' }
  ];

  priorityOptions = [
    { value: 'high', label: 'High Priority' },
    { value: 'medium', label: 'Medium Priority' },
    { value: 'low', label: 'Low Priority' }
  ];

  constructor(private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      dateFrom: [null],
      dateTo: [null],
      severity: [''],
      priority: [''],
      searchText: ['']
    });
  }

  ngOnInit(): void {
    // Subscribe to form changes with debounce for search text
    this.filterForm.valueChanges
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(300),
        distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr))
      )
      .subscribe(value => {
        this.emitFilterChange(value);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private emitFilterChange(formValue: any): void {
    const filter: LogFilter = {
      dateFrom: formValue.dateFrom || undefined,
      dateTo: formValue.dateTo || undefined,
      severity: formValue.severity || undefined,
      priority: formValue.priority || undefined,
      searchText: formValue.searchText?.trim() || undefined
    };

    // Only emit if there are actual filter values
    const hasFilters = Object.values(filter).some(value => value !== undefined && value !== '');
    
    if (hasFilters) {
      this.filterChange.emit(filter);
    } else {
      this.filterChange.emit({});
    }
  }

  onClearFilters(): void {
    this.filterForm.reset({
      dateFrom: null,
      dateTo: null,
      severity: '',
      priority: '',
      searchText: ''
    });
    
    this.clearFilters.emit();
    this.filterChange.emit({});
  }

  hasActiveFilters(): boolean {
    const formValue = this.filterForm.value;
    return Object.values(formValue).some(value => 
      value !== null && value !== undefined && value !== ''
    );
  }

  getActiveFilterCount(): number {
    const formValue = this.filterForm.value;
    return Object.values(formValue).filter(value => 
      value !== null && value !== undefined && value !== ''
    ).length;
  }

  onDateFromChange(): void {
    const dateFrom = this.filterForm.get('dateFrom')?.value;
    const dateTo = this.filterForm.get('dateTo')?.value;
    
    // If dateFrom is after dateTo, clear dateTo
    if (dateFrom && dateTo && dateFrom > dateTo) {
      this.filterForm.patchValue({ dateTo: null });
    }
  }

  onDateToChange(): void {
    const dateFrom = this.filterForm.get('dateFrom')?.value;
    const dateTo = this.filterForm.get('dateTo')?.value;
    
    // If dateTo is before dateFrom, clear dateFrom
    if (dateFrom && dateTo && dateTo < dateFrom) {
      this.filterForm.patchValue({ dateFrom: null });
    }
  }
}
