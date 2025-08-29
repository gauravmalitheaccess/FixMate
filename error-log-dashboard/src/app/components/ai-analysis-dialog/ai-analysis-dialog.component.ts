import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AzureAiServicepService, SendMessageRequest } from '../../services/azure-ai-servicep.service';

export interface AiAnalysisDialogData {
  // Optional pre-filled stack trace
  stackTrace?: string;
}

export interface RawExceptionData {
  source: string;
  message: string;
  stackTrace?: string;
  context?: string;
  userId?: string;
  sessionId?: string;
  appVersion?: string;
  environment?: string;
}

@Component({
  selector: 'app-ai-analysis-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule
  ],
  templateUrl: './ai-analysis-dialog.component.html',
  styleUrl: './ai-analysis-dialog.component.scss'
})
export class AiAnalysisDialogComponent {
  stackTraceForm: FormGroup;
  isAnalyzing = false;

  exampleStackTraces: any[] = [];

  constructor(
    private fb: FormBuilder,
    private aiService: AzureAiServicepService,
    public dialogRef: MatDialogRef<AiAnalysisDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: AiAnalysisDialogData
  ) {
    this.stackTraceForm = this.fb.group({
      stackTrace: [data?.stackTrace || '', [Validators.required, Validators.maxLength(10000)]]
    });
  }

  loadStackTraceExample(stackTrace: string): void {
    this.stackTraceForm.patchValue({ stackTrace });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onAnalyze(): void {



    if (this.stackTraceForm.valid) {
      this.isAnalyzing = true;
      const stackTrace = this.stackTraceForm.get('stackTrace')?.value;
      
      this.isAnalyzing = true;
      
      // Optionally, extract source/message from stack trace if needed
      const { source, message } = this.parseStackTrace(stackTrace);
  
      const request: SendMessageRequest = {
        assistantId: 'asst_WTnoBCs0I8ICox3XyujVKc24', // replace with actual Assistant ID
        message: stackTrace
      };
  
      // Call API
      this.aiService.sendMessage(request).subscribe({
        next: (response) => {
          console.log('AI Response:', response);
  
          const rawException: RawExceptionData = {
            source,
            message,
            stackTrace
          };
          this.isAnalyzing = false;
          this.dialogRef.close(rawException);
        },
        error: (err) => {
          console.error('Error calling Azure AI API:', err);
          this.isAnalyzing = false;
          // fallback: still return raw exception
          this.dialogRef.close({ source, message, stackTrace });
        }
      });
    } else {
      this.stackTraceForm.markAllAsTouched();
    }
  }

  private parseStackTrace(stackTrace: string): { source: string; message: string } {
    const lines = stackTrace.split('\n').map(line => line.trim()).filter(line => line);
    
    let message = 'Unknown error';
    let source = 'Unknown';
    
    // Try to extract exception message from first line
    if (lines.length > 0) {
      const firstLine = lines[0];
      const colonIndex = firstLine.indexOf(':');
      if (colonIndex > 0) {
        message = firstLine.substring(colonIndex + 1).trim();
      } else {
        message = firstLine;
      }
    }
    
    // Try to extract source from stack trace lines
    for (const line of lines) {
      if (line.includes('at ') && line.includes('(')) {
        const atIndex = line.indexOf('at ');
        const parenIndex = line.indexOf('(');
        if (atIndex >= 0 && parenIndex > atIndex) {
          const methodCall = line.substring(atIndex + 3, parenIndex).trim();
          // Take the last part as the source (class.method)
          const parts = methodCall.split('.');
          if (parts.length >= 2) {
            source = `${parts[parts.length - 2]}.${parts[parts.length - 1]}`;
            break;
          }
        }
      }
    }
    
    return { source, message };
  }

  getFieldError(fieldName: string): string {
    const field = this.stackTraceForm.get(fieldName);
    if (field?.errors && field.touched) {
      if (field.errors['required']) {
        return `Exception is required`;
      }
      if (field.errors['maxlength']) {
        return `Exception is too long (max ${field.errors['maxlength'].requiredLength} characters)`;
      }
    }
    return '';
  }
}
