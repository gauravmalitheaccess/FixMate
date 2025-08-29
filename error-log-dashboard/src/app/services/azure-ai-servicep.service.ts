import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface SendMessageRequest {
  assistantId: string;
  message: string;
}
@Injectable({
  providedIn: 'root'
})
export class AzureAiServicepService {

  private readonly apiUrl = 'https://localhost:44314/api/azureai';

  constructor(private http: HttpClient) { }

  sendMessage(request: SendMessageRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/send-message`, request);
  }
}
