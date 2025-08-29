# Error Log Prioritization System - Deployment Guide

## Overview

This guide provides step-by-step instructions for deploying the Error Log Prioritization System in different environments (Development, Staging, Production).

## Prerequisites

### System Requirements
- .NET 8.0 SDK or later
- Node.js 18+ and npm
- Windows Server 2019+ or Linux (Ubuntu 20.04+)
- Minimum 4GB RAM, 2 CPU cores
- 10GB available disk space

### External Dependencies
- Microsoft Copilot Studio API access
- Valid API key and endpoint URL
- Network access to Copilot Studio endpoints

## Environment Setup

### Development Environment

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd error-log-prioritization
   ```

2. **Configure Development Settings**
   - Copy `appsettings.Development.json.template` to `appsettings.Development.json`
   - Update the following configuration values:
   ```json
   {
     "CopilotStudio": {
       "BaseUrl": "https://your-dev-copilot-studio-endpoint.com",
       "ApiUrl": "https://your-dev-copilot-studio-endpoint.com/api/analyze",
       "ApiKey": "your-development-api-key"
     },
     "FileStorage": {
       "LogsPath": "Data/Dev/Logs",
       "ExportsPath": "Data/Dev/Exports"
     }
   }
   ```

3. **Install Backend Dependencies**
   ```bash
   cd ErrorLogPrioritization.Api
   dotnet restore
   dotnet build
   ```

4. **Install Frontend Dependencies**
   ```bash
   cd error-log-dashboard
   npm install
   ```

5. **Run the Application**
   ```bash
   # Terminal 1 - Backend API
   cd ErrorLogPrioritization.Api
   dotnet run

   # Terminal 2 - Frontend Dashboard
   cd error-log-dashboard
   npm start
   ```

6. **Verify Installation**
   - Backend API: http://localhost:5000/health
   - Frontend Dashboard: http://localhost:4200
   - Hangfire Dashboard: http://localhost:5000/hangfire

### Staging Environment

1. **Server Preparation**
   ```bash
   # Install .NET 8 Runtime
   wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt-get update
   sudo apt-get install -y aspnetcore-runtime-8.0

   # Install Node.js
   curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
   sudo apt-get install -y nodejs

   # Install Nginx (for reverse proxy)
   sudo apt-get install -y nginx
   ```

2. **Application Deployment**
   ```bash
   # Build and publish backend
   cd ErrorLogPrioritization.Api
   dotnet publish -c Release -o /var/www/errorlog-api

   # Build frontend
   cd error-log-dashboard
   npm run build
   sudo cp -r dist/* /var/www/errorlog-dashboard/
   ```

3. **Configuration**
   - Update `appsettings.Staging.json` with staging-specific values
   - Configure Nginx reverse proxy:
   ```nginx
   server {
       listen 80;
       server_name your-staging-domain.com;

       location /api/ {
           proxy_pass http://localhost:5000/api/;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }

       location / {
           root /var/www/errorlog-dashboard;
           try_files $uri $uri/ /index.html;
       }
   }
   ```

4. **Service Configuration**
   ```bash
   # Create systemd service
   sudo nano /etc/systemd/system/errorlog-api.service
   ```
   ```ini
   [Unit]
   Description=Error Log Prioritization API
   After=network.target

   [Service]
   Type=notify
   ExecStart=/usr/bin/dotnet /var/www/errorlog-api/ErrorLogPrioritization.Api.dll
   Restart=always
   RestartSec=5
   Environment=ASPNETCORE_ENVIRONMENT=Staging
   Environment=ASPNETCORE_URLS=http://localhost:5000
   WorkingDirectory=/var/www/errorlog-api

   [Install]
   WantedBy=multi-user.target
   ```

   ```bash
   sudo systemctl enable errorlog-api
   sudo systemctl start errorlog-api
   ```

### Production Environment

1. **Infrastructure Setup**
   - Use load balancer for high availability
   - Configure SSL certificates
   - Set up monitoring and logging
   - Configure backup strategy for log files

2. **Security Configuration**
   ```json
   {
     "CopilotStudio": {
       "ApiKey": "production-api-key-from-secure-vault"
     },
     "FileStorage": {
       "LogsPath": "/var/log/errorlog-prioritization/logs",
       "ExportsPath": "/var/log/errorlog-prioritization/exports"
     },
     "Serilog": {
       "MinimumLevel": {
         "Default": "Warning"
       }
     }
   }
   ```

3. **Performance Optimization**
   - Configure connection pooling
   - Set up file rotation and cleanup
   - Configure caching strategies
   - Monitor memory usage and performance metrics

4. **Monitoring Setup**
   ```bash
   # Install monitoring tools
   sudo apt-get install -y prometheus node-exporter
   
   # Configure health check monitoring
   curl -f http://localhost:5000/health || exit 1
   ```

## Configuration Reference

### Required Configuration Sections

#### CopilotStudio Configuration
```json
{
  "CopilotStudio": {
    "BaseUrl": "https://your-copilot-studio-endpoint.com",
    "ApiUrl": "https://your-copilot-studio-endpoint.com/api/analyze",
    "ApiKey": "your-api-key",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

#### File Storage Configuration
```json
{
  "FileStorage": {
    "LogsPath": "Data/Logs",
    "ExportsPath": "Data/Exports",
    "RetentionDays": 30,
    "MaxFileSizeMB": 100,
    "CreateDirectoriesIfNotExist": true
  }
}
```

#### Scheduling Configuration
```json
{
  "Scheduling": {
    "DailyAnalysisTime": "01:00:00",
    "RetryIntervalMinutes": 30,
    "MaxRetryAttempts": 3,
    "EnableScheduledAnalysis": true,
    "TimeZone": "UTC"
  }
}
```

#### Performance Configuration
```json
{
  "Performance": {
    "MaxLogsPerRequest": 10000,
    "CacheExpirationSeconds": 300,
    "MaxConcurrentAnalysis": 5,
    "BatchSizeForProcessing": 100,
    "EnablePerformanceMonitoring": true
  }
}
```

## Environment Variables

Set the following environment variables for production:

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000
export COPILOT_STUDIO_API_KEY=your-production-api-key
export FILE_STORAGE_LOGS_PATH=/var/log/errorlog-prioritization/logs
export FILE_STORAGE_EXPORTS_PATH=/var/log/errorlog-prioritization/exports
```

## Health Checks

The application provides several health check endpoints:

- `/health` - Overall application health
- `/health/ready` - Readiness probe for Kubernetes
- `/health/live` - Liveness probe for Kubernetes

Health checks verify:
- File system accessibility
- Copilot Studio API connectivity
- Background job scheduler status

## Backup and Recovery

### Log Files Backup
```bash
# Daily backup script
#!/bin/bash
DATE=$(date +%Y%m%d)
tar -czf /backup/logs-$DATE.tar.gz /var/log/errorlog-prioritization/logs/
find /backup -name "logs-*.tar.gz" -mtime +30 -delete
```

### Configuration Backup
```bash
# Backup configuration files
cp /var/www/errorlog-api/appsettings.Production.json /backup/config/
```

## Troubleshooting

### Common Issues

1. **Configuration Validation Errors**
   - Check application logs for specific validation failures
   - Verify all required configuration sections are present
   - Ensure API keys and URLs are correctly formatted

2. **File Permission Issues**
   ```bash
   sudo chown -R www-data:www-data /var/log/errorlog-prioritization/
   sudo chmod -R 755 /var/log/errorlog-prioritization/
   ```

3. **Copilot Studio Connection Issues**
   - Verify API key is valid and not expired
   - Check network connectivity to Copilot Studio endpoints
   - Review firewall rules and proxy settings

4. **Performance Issues**
   - Monitor memory usage and adjust batch sizes
   - Check disk space for log storage
   - Review scheduled job execution times

### Log Analysis
```bash
# View application logs
sudo journalctl -u errorlog-api -f

# Check specific error patterns
grep -i "error\|exception" /var/log/errorlog-prioritization/logs/log-*.txt
```

## Scaling Considerations

### Horizontal Scaling
- Deploy multiple API instances behind a load balancer
- Use shared file storage (NFS, Azure Files, etc.)
- Configure sticky sessions for Hangfire dashboard

### Vertical Scaling
- Monitor CPU and memory usage
- Adjust batch processing sizes based on available resources
- Configure appropriate timeout values for large datasets

## Security Best Practices

1. **API Security**
   - Use HTTPS in production
   - Implement rate limiting
   - Configure CORS appropriately
   - Use secure API key storage (Azure Key Vault, etc.)

2. **File Security**
   - Restrict file system permissions
   - Encrypt sensitive log data
   - Implement log retention policies
   - Regular security audits

3. **Network Security**
   - Use VPN or private networks for Copilot Studio communication
   - Configure firewall rules
   - Monitor network traffic

## Integration Testing and Performance Validation

### Pre-Deployment Testing

Before deploying to any environment, run the comprehensive test suite to ensure system reliability:

1. **Run Complete Test Suite**
   ```bash
   # Backend API Tests
   cd ErrorLogPrioritization.Api.Tests
   dotnet test --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"
   
   # Frontend Tests
   cd error-log-dashboard
   npm test -- --watch=false --browsers=ChromeHeadless --code-coverage
   
   # End-to-End Tests
   npm run e2e
   ```

2. **Load Testing Validation**
   ```bash
   # Test with 10,000+ log entries
   cd ErrorLogPrioritization.Api.Tests
   dotnet test --filter "Category=LoadTest" --logger console
   
   # Performance benchmarks must meet:
   # - Log collection: 1000 logs/second
   # - Log retrieval: 10,000 logs in <3 seconds  
   # - Excel export: 10,000 logs in <30 seconds
   # - Memory usage: <2GB for 100,000 logs
   ```

3. **Integration Workflow Testing**
   ```bash
   # Complete user workflow tests
   dotnet test --filter "FullyQualifiedName~CompleteWorkflowE2ETests"
   
   # Scheduled analysis workflow
   dotnet test --filter "FullyQualifiedName~ScheduledAnalysisIntegrationTests"
   
   # Excel export functionality
   dotnet test --filter "FullyQualifiedName~ExcelDownloadIntegrationTests"
   ```

### Performance Requirements Validation

The system must meet these performance benchmarks in production:

#### Response Time Requirements
- **Dashboard Load**: <3 seconds for 10,000 log entries
- **Log Filtering**: <2 seconds for complex filters
- **Excel Export**: <30 seconds for 10,000 logs
- **API Health Check**: <500ms response time

#### Throughput Requirements  
- **Log Collection**: Handle 1,000 logs/second sustained
- **Concurrent Users**: Support 100 concurrent dashboard users
- **Scheduled Analysis**: Process 50,000 logs within 30 minutes
- **File Operations**: Handle 100MB/s disk I/O

#### Resource Usage Limits
- **Memory**: <2GB RAM for 100,000 active logs
- **CPU**: <80% utilization under normal load
- **Disk Space**: <1GB per 100,000 logs (with compression)
- **Network**: <10MB/s bandwidth for normal operations

### Performance Monitoring Setup

1. **Application Performance Monitoring**
   ```bash
   # Install performance monitoring tools
   sudo apt-get install -y htop iotop nethogs
   
   # Configure system monitoring
   cat > /etc/systemd/system/performance-monitor.service << EOF
   [Unit]
   Description=Performance Monitor for Error Log System
   After=network.target
   
   [Service]
   Type=simple
   ExecStart=/usr/local/bin/monitor-performance.sh
   Restart=always
   RestartSec=60
   
   [Install]
   WantedBy=multi-user.target
   EOF
   ```

2. **Performance Monitoring Script**
   ```bash
   # Create /usr/local/bin/monitor-performance.sh
   #!/bin/bash
   
   LOG_FILE="/var/log/errorlog-performance.log"
   API_URL="http://localhost:5000"
   
   while true; do
       TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
       
       # Check API response time
       RESPONSE_TIME=$(curl -w "%{time_total}" -o /dev/null -s "$API_URL/health")
       
       # Check memory usage
       MEMORY_USAGE=$(free | grep Mem | awk '{printf "%.1f", $3/$2 * 100.0}')
       
       # Check disk usage
       DISK_USAGE=$(df /var/log/errorlog-prioritization | tail -1 | awk '{print $5}' | sed 's/%//')
       
       # Log metrics
       echo "$TIMESTAMP,API_RESPONSE:${RESPONSE_TIME}s,MEMORY:${MEMORY_USAGE}%,DISK:${DISK_USAGE}%" >> $LOG_FILE
       
       # Alert if thresholds exceeded
       if (( $(echo "$RESPONSE_TIME > 1.0" | bc -l) )); then
           echo "ALERT: API response time exceeded 1s: ${RESPONSE_TIME}s" | logger
       fi
       
       if (( MEMORY_USAGE > 80 )); then
           echo "ALERT: Memory usage high: ${MEMORY_USAGE}%" | logger
       fi
       
       if (( DISK_USAGE > 85 )); then
           echo "ALERT: Disk usage high: ${DISK_USAGE}%" | logger
       fi
       
       sleep 60
   done
   ```

3. **Load Testing in Production**
   ```bash
   # Create load test script for production validation
   cat > /usr/local/bin/production-load-test.sh << EOF
   #!/bin/bash
   
   echo "Starting production load test..."
   
   # Test log collection endpoint
   echo "Testing log collection performance..."
   ab -n 1000 -c 10 -T 'application/json' -p /tmp/test-log.json http://localhost:5000/api/log/collect
   
   # Test log retrieval performance  
   echo "Testing log retrieval performance..."
   ab -n 500 -c 20 http://localhost:5000/api/log?pageSize=1000
   
   # Test Excel export performance
   echo "Testing Excel export performance..."
   time curl -o /tmp/test-export.xlsx "http://localhost:5000/api/log/export/$(date +%Y-%m-%d)"
   
   echo "Load test completed. Check results above."
   EOF
   
   chmod +x /usr/local/bin/production-load-test.sh
   ```

### Deployment Validation Checklist

Before marking a deployment as successful, verify:

#### Functional Tests
- [ ] API health check returns 200 OK
- [ ] Dashboard loads within 3 seconds
- [ ] Log collection endpoint accepts test data
- [ ] Scheduled analysis job executes successfully
- [ ] Excel export generates valid files
- [ ] Error handling works for invalid requests
- [ ] Authentication/authorization (if enabled)

#### Performance Tests  
- [ ] Load test with 1000 concurrent log submissions passes
- [ ] Dashboard responds within 3 seconds with 10,000 logs
- [ ] Excel export completes within 30 seconds for 10,000 logs
- [ ] Memory usage stays below 2GB under load
- [ ] CPU usage stays below 80% under normal load
- [ ] Disk I/O performance meets 100MB/s requirement

#### Integration Tests
- [ ] Complete workflow (collection → analysis → dashboard → export) works
- [ ] Copilot Studio integration functions correctly
- [ ] Scheduled jobs execute on time
- [ ] File storage operations work correctly
- [ ] Error recovery mechanisms function properly

#### Security Tests
- [ ] HTTPS configuration works correctly
- [ ] API rate limiting functions
- [ ] File permissions are properly restricted
- [ ] Sensitive data is not exposed in logs
- [ ] Network security rules are applied

### Troubleshooting Performance Issues

#### High Memory Usage
```bash
# Check memory usage by process
ps aux --sort=-%mem | head -10

# Check for memory leaks
dotnet-dump collect -p $(pgrep -f ErrorLogPrioritization.Api)
dotnet-dump analyze <dump-file>

# Optimize garbage collection
export DOTNET_gcServer=1
export DOTNET_gcConcurrent=1
```

#### Slow API Response Times
```bash
# Check database/file I/O performance
iotop -o

# Monitor network latency to Copilot Studio
ping -c 10 your-copilot-studio-endpoint.com

# Check for blocking operations
dotnet-trace collect -p $(pgrep -f ErrorLogPrioritization.Api) --duration 00:00:30
```

#### Disk Space Issues
```bash
# Check log file sizes
du -sh /var/log/errorlog-prioritization/*

# Clean up old files
find /var/log/errorlog-prioritization -name "*.json" -mtime +30 -delete

# Compress old logs
find /var/log/errorlog-prioritization -name "*.json" -mtime +7 -exec gzip {} \;
```

## Maintenance

### Regular Tasks
- Monitor disk usage and clean up old log files
- Update API keys before expiration
- Review and update configuration settings
- Monitor application performance metrics
- Update dependencies and security patches
- Run weekly performance validation tests
- Review system logs for errors and warnings

### Scheduled Maintenance
- **Daily**: Run automated performance monitoring
- **Weekly**: Review error logs and performance metrics, run load tests
- **Monthly**: Update dependencies and security patches, performance optimization review
- **Quarterly**: Review and optimize configuration settings, capacity planning
- **Annually**: Security audit and penetration testing, disaster recovery testing

### Performance Optimization Tasks
- **Monitor API response times** and optimize slow endpoints
- **Review memory usage patterns** and adjust garbage collection settings
- **Analyze disk I/O performance** and optimize file operations
- **Monitor Copilot Studio response times** and adjust timeout settings
- **Review log file sizes** and implement compression/archival strategies
- **Test system limits** with increasing load to plan for scaling