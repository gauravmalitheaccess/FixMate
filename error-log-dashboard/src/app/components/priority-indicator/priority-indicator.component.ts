import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-priority-indicator',
  standalone: true,
  imports: [
    CommonModule,
    MatChipsModule,
    MatTooltipModule,
    MatIconModule
  ],
  templateUrl: './priority-indicator.component.html',
  styleUrl: './priority-indicator.component.scss'
})
export class PriorityIndicatorComponent {
  @Input() priority: string = '';
  @Input() aiReasoning: string = '';
  @Input() size: 'small' | 'medium' | 'large' = 'medium';

  getPriorityConfig() {
    const priority = this.priority?.toLowerCase() || 'unknown';
    
    const configs = {
      high: {
        color: '#d32f2f', // Red for High priority
        backgroundColor: '#ffebee',
        icon: 'priority_high',
        label: 'High'
      },
      medium: {
        color: '#ff9800', // Orange for Medium priority
        backgroundColor: '#fff3e0',
        icon: 'remove',
        label: 'Medium'
      },
      low: {
        color: '#4caf50', // Green for Low priority
        backgroundColor: '#e8f5e8',
        icon: 'keyboard_arrow_down',
        label: 'Low'
      },
      unknown: {
        color: '#9e9e9e',
        backgroundColor: '#f5f5f5',
        icon: 'help_outline',
        label: 'Unknown'
      }
    };

    return configs[priority as keyof typeof configs] || configs.unknown;
  }

  getTooltipText(): string {
    const config = this.getPriorityConfig();
    let tooltip = config.label;
    
    if (this.aiReasoning) {
      tooltip += `\n\nAI Reasoning: ${this.aiReasoning}`;
    }
    
    return tooltip;
  }

  getSizeClass(): string {
    return `priority-${this.size}`;
  }
}
