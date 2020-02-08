﻿namespace Framework.Api.Base.Logging
{
    using System;
    using Newtonsoft.Json.Linq;

    /*
     * Each API request writes a structured log entry containing fields we will query by
     * It also writes JSON blobs whose fields are not designed to be queried
     */
    public class LogEntryData
    {
        public LogEntryData()
        {
            // Queryable fields
            this.Id = Guid.NewGuid().ToString();
            this.UtcTime = DateTime.UtcNow;
            this.ApiName = string.Empty;
            this.OperationName = string.Empty;
            this.HostName = string.Empty;
            this.RequestVerb = string.Empty;
            this.ResourceId = string.Empty;
            this.RequestPath = string.Empty;
            this.ClientId = string.Empty;
            this.CallingApplicationName = string.Empty;
            this.UserId = string.Empty;
            this.UserName = string.Empty;
            this.StatusCode = 0;
            this.MillisecondsTaken = 0;
            this.PerformanceThresholdMilliseconds = 0;
            this.ErrorCode = string.Empty;
            this.ErrorId = 0;
            this.CorrelationId = string.Empty;
            this.SessionId = string.Empty;

            // Objects that are not directly queryable
            this.Performance = new PerformanceBreakdown("total");
            this.ErrorData = null;
            this.InfoData = new JArray();
        }

        // A unique generated client side id, which becomes the unique id in the aggregated logs database
        public string Id { get; set; }

        // The time when the API received the request
        public DateTime UtcTime { get; set; }

        // The name of the API
        public string ApiName { get; set; }

        // The operation called
        public string OperationName { get; set; }

        // The host on which the request was processed
        public string HostName { get; set; }

        // The HTTP verb
        public string RequestVerb { get; set; }

        // The resource id(s) in the request URL path segments is often useful to query by
        public string ResourceId { get; set; }

        // The request path
        public string RequestPath { get; set; }

        // The application id of the original caller of the consumer API
        public string ClientId { get; set; }

        // The calling application name
        public string CallingApplicationName { get; set; }

        // The calling user, for secured requests
        public string UserId { get; set; }

        // The calling user name, for secured requests
        public string UserName { get; set; }

        // The status code returned
        public int StatusCode { get; set; }

        // The time taken in API code
        public int MillisecondsTaken { get; set; }

        // A time beyond which performance is considered 'slow'
        public int PerformanceThresholdMilliseconds { get; set; }

        // The error code for requests that failed
        public string ErrorCode { get; set; }

        // The specific error instance id, for 500 errors
        public int ErrorId { get; set; }

        // The correlation id, used to link related API requests together
        public string CorrelationId { get; set; }

        // A session id, to group related calls from a client together
        public string SessionId { get; set; }

        // An object containing performance data, written when performance is slow
        public PerformanceBreakdown Performance { get; private set;  }

        // An object containing error data, written for failed requests
        public JObject ErrorData { get; private set; }

        // Can be populated in scenarios when extra text is useful
        public JArray InfoData { get; private set; }

        /*
        * Set fields at the end of a log entry
        */
        public void Finalise()
        {
            this.MillisecondsTaken = this.Performance.MillisecondsTaken;
        }

        /*
        * For child items, this receives common properties from the parent
        */
        public void UpdateFromParent(LogEntryData parent)
        {
            this.StatusCode = parent.StatusCode;
            this.ApiName = parent.ApiName;
            this.HostName = parent.HostName;
            this.RequestVerb = parent.RequestVerb;
            this.ResourceId = parent.ResourceId;
            this.RequestPath = parent.RequestPath;
            this.ClientId = parent.ClientId;
            this.CallingApplicationName = parent.CallingApplicationName;
            this.UserId = parent.UserId;
            this.UserName = parent.UserName;
            this.CorrelationId = parent.CorrelationId;
            this.SessionId = parent.SessionId;
        }

        /*
        * This updates the parent log entry when the child log entry completes
        */
        public void UpdateFromChild(LogEntryData child)
        {
            // If the child fails, also indicate a parent failure
            if (string.IsNullOrWhiteSpace(this.ErrorCode) && !string.IsNullOrWhiteSpace(child.ErrorCode))
            {
                this.ErrorCode = child.ErrorCode;
            }

            // Exclude child execution time from the parent time
            this.Performance.MillisecondsTaken -= child.MillisecondsTaken;
            this.MillisecondsTaken -= child.MillisecondsTaken;
        }

        /*
        * Produce the output format
        */
        public JObject ToLogFormat()
        {
            // Output fields used as top level queryable columns
            dynamic output = new JObject();
            this.OutputString((x) => output.id = x, this.Id);
            this.OutputString((x) => output.utcTime = x, this.UtcTime.ToString("s"));
            this.OutputString((x) => output.apiName = x, this.ApiName);
            this.OutputString((x) => output.operationName = x, this.OperationName);
            this.OutputString((x) => output.hostName = x, this.HostName);
            this.OutputString((x) => output.requestVerb = x, this.RequestVerb);
            this.OutputString((x) => output.resourceId = x, this.ResourceId);
            this.OutputString((x) => output.requestPath = x, this.RequestPath);
            this.OutputString((x) => output.clientId = x, this.ClientId);
            this.OutputString((x) => output.callingApplicationName = x, this.CallingApplicationName);
            this.OutputString((x) => output.userId = x, this.UserId);
            this.OutputString((x) => output.userName = x, this.UserName);
            this.OutputNumber((x) => output.statusCode = x, this.StatusCode);
            this.OutputString((x) => output.errorCode = x, this.ErrorCode);
            this.OutputNumber((x) => output.errorId = x, this.ErrorId);
            this.OutputNumber((x) => output.millisecondsTaken = x, this.Performance.MillisecondsTaken);
            this.OutputNumber((x) => output.millisecondsThreshold = x, this.PerformanceThresholdMilliseconds, true);
            this.OutputString((x) => output.correlationId = x, this.CorrelationId);
            this.OutputString((x) => output.sessionId = x, this.SessionId);

            // Output object data, which is looked up via top level fields
            this.OutputPerformance(output);
            this.OutputError(output);
            this.OutputInfo(output);
            return output;
        }

        /*
        * Indicate whether an error entry
        */
        public bool IsError()
        {
            return this.ErrorData != null;
        }

        /*
        * Add a string to the output unless empty
        */
        private void OutputString(Action<string> setter, string value)
        {
            if (value.Length > 0)
            {
                setter(value);
            }
        }

        /*
        * Add a number to the output unless zero or forced
        */
        private void OutputNumber(Action<int> setter, int value, bool force = false)
        {
            if (value > 0 || force)
            {
                setter(value);
            }
        }

        /*
        * Add the performance breakdown if the threshold has been exceeded or there has been a 500 error
        */
        private void OutputPerformance(dynamic output)
        {
            if (this.Performance.MillisecondsTaken >= this.PerformanceThresholdMilliseconds || this.ErrorId > 0)
            {
                output.Performance = this.Performance.GetData();
            }
        }

        /*
        * Add error details if applicable
        */
        private void OutputError(dynamic output)
        {
            if (this.ErrorData != null)
            {
                output.ErrorData = this.ErrorData;
            }
        }

        /*
        * Add info details if applicable
        */
        private void OutputInfo(dynamic data)
        {
            if (this.InfoData.Count > 0)
            {
                data.InfoData = this.InfoData;
            }
        }
    }
}
