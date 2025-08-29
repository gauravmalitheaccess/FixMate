import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { PriorityIndicatorComponent } from './priority-indicator.component';

describe('PriorityIndicatorComponent', () => {
  let component: PriorityIndicatorComponent;
  let fixture: ComponentFixture<PriorityIndicatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        PriorityIndicatorComponent,
        MatChipsModule,
        MatTooltipModule,
        MatIconModule,
        NoopAnimationsModule
      ]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(PriorityIndicatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('getPriorityConfig', () => {
    it('should return high priority config for high priority', () => {
      component.priority = 'high';
      const config = component.getPriorityConfig();
      
      expect(config.color).toBe('#d32f2f'); // Red for High priority
      expect(config.backgroundColor).toBe('#ffebee');
      expect(config.icon).toBe('priority_high');
      expect(config.label).toBe('High Priority');
    });

    it('should return medium priority config for medium priority', () => {
      component.priority = 'medium';
      const config = component.getPriorityConfig();
      
      expect(config.color).toBe('#ff9800'); // Orange for Medium priority
      expect(config.backgroundColor).toBe('#fff3e0');
      expect(config.icon).toBe('remove');
      expect(config.label).toBe('Medium Priority');
    });

    it('should return low priority config for low priority', () => {
      component.priority = 'low';
      const config = component.getPriorityConfig();
      
      expect(config.color).toBe('#4caf50'); // Green for Low priority
      expect(config.backgroundColor).toBe('#e8f5e8');
      expect(config.icon).toBe('keyboard_arrow_down');
      expect(config.label).toBe('Low Priority');
    });

    it('should return unknown config for invalid priority', () => {
      component.priority = 'invalid';
      const config = component.getPriorityConfig();
      
      expect(config.color).toBe('#9e9e9e');
      expect(config.backgroundColor).toBe('#f5f5f5');
      expect(config.icon).toBe('help_outline');
      expect(config.label).toBe('Unknown Priority');
    });

    it('should handle case insensitive priority values', () => {
      component.priority = 'HIGH';
      const config = component.getPriorityConfig();
      
      expect(config.label).toBe('High Priority');
    });

    it('should handle empty priority', () => {
      component.priority = '';
      const config = component.getPriorityConfig();
      
      expect(config.label).toBe('Unknown Priority');
    });
  });

  describe('getTooltipText', () => {
    it('should return priority label when no AI reasoning provided', () => {
      component.priority = 'high';
      component.aiReasoning = '';
      
      const tooltip = component.getTooltipText();
      expect(tooltip).toBe('High Priority');
    });

    it('should include AI reasoning when provided', () => {
      component.priority = 'high';
      component.aiReasoning = 'Critical database connection error affecting multiple users';
      
      const tooltip = component.getTooltipText();
      expect(tooltip).toBe('High Priority\n\nAI Reasoning: Critical database connection error affecting multiple users');
    });

    it('should handle missing priority with AI reasoning', () => {
      component.priority = '';
      component.aiReasoning = 'Some reasoning';
      
      const tooltip = component.getTooltipText();
      expect(tooltip).toBe('Unknown Priority\n\nAI Reasoning: Some reasoning');
    });
  });

  describe('getSizeClass', () => {
    it('should return correct class for small size', () => {
      component.size = 'small';
      expect(component.getSizeClass()).toBe('priority-small');
    });

    it('should return correct class for medium size', () => {
      component.size = 'medium';
      expect(component.getSizeClass()).toBe('priority-medium');
    });

    it('should return correct class for large size', () => {
      component.size = 'large';
      expect(component.getSizeClass()).toBe('priority-large');
    });

    it('should default to medium size', () => {
      expect(component.getSizeClass()).toBe('priority-medium');
    });
  });

  describe('component rendering', () => {
    it('should display priority chip with correct styling', () => {
      component.priority = 'high';
      component.aiReasoning = 'Test reasoning';
      fixture.detectChanges();

      const chipElement = fixture.nativeElement.querySelector('mat-chip');
      expect(chipElement).toBeTruthy();
      expect(chipElement.textContent.trim()).toContain('High Priority');
    });

    it('should show tooltip on hover', () => {
      component.priority = 'medium';
      component.aiReasoning = 'Test reasoning';
      fixture.detectChanges();

      // Test the tooltip text method directly
      const tooltipText = component.getTooltipText();
      expect(tooltipText).toContain('Medium Priority');
      expect(tooltipText).toContain('Test reasoning');
    });

    it('should display correct icon for priority level', () => {
      component.priority = 'high';
      fixture.detectChanges();

      const iconElement = fixture.nativeElement.querySelector('mat-icon');
      expect(iconElement.textContent.trim()).toBe('priority_high');
    });
  });
});
