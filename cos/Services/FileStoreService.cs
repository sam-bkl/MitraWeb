using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace cos.Services
{
    public class FileStoreService
    {
        private readonly HttpClient _httpClient;
        private readonly string _uploadBaseUrl;
        private readonly string _retrieveBaseUrl;
        private readonly string _storageCategory;

        public FileStoreService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            
            _uploadBaseUrl = configuration.GetValue<string>("FileStore:UploadUrl") ?? "http://10.201.222.68:9810/upload";
            _retrieveBaseUrl = configuration.GetValue<string>("FileStore:RetrieveUrl") ?? "http://10.201.222.68:9801";
            _storageCategory = configuration.GetValue<string>("FileStore:StorageCategory") ?? "ctopup-master-user-docs";
        }

        public class UploadResponse
        {
            public long file_size_in_bytes { get; set; }
            public string? url { get; set; }
        }

        public async Task<UploadResponse> UploadFileAsync(IFormFile file, DateTime uploadDate)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                
                // Format date as YYYY/MM/DD
                var dateString = uploadDate.ToString("yyyy/MM/dd");
                content.Add(new StringContent(dateString), "date");
                content.Add(new StringContent(_storageCategory), "storage_category");
                
                // Add file
                using var fileStream = file.OpenReadStream();
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.FileName);

                var response = await _httpClient.PostAsync(_uploadBaseUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"File Store API upload failed: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (uploadResponse == null)
                {
                    throw new Exception("Invalid response from File Store API");
                }

                return uploadResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error while uploading to File Store API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Timeout while uploading to File Store API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file to File Store API: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> RetrieveFileAsync(string filename, DateTime uploadDate)
        {
            try
            {
                // Format date as YYYY/MM/DD
                var dateString = uploadDate.ToString("yyyy/MM/dd");
                var url = $"{_retrieveBaseUrl}/{_storageCategory}/{dateString}/{filename}";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new FileNotFoundException($"File not found in File Store: {filename}");
                    }
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"File Store API retrieval failed: {response.StatusCode} - {errorContent}");
                }

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error while retrieving from File Store API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Timeout while retrieving from File Store API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving file from File Store API: {ex.Message}", ex);
            }
        }

        public string BuildRetrieveUrl(string filename, DateTime uploadDate)
        {
            var dateString = uploadDate.ToString("yyyy/MM/dd");
            return $"{_retrieveBaseUrl}/{_storageCategory}/{dateString}/{filename}";
        }
    }
}

