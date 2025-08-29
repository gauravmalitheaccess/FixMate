import { TestBed } from '@angular/core/testing';

import { AzureAiServicepService } from './azure-ai-servicep.service';

describe('AzureAiServicepService', () => {
  let service: AzureAiServicepService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AzureAiServicepService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
