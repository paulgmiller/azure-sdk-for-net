﻿// -----------------------------------------------------------------------------------------
// <copyright file="CloudBlobContainerTest.cs" company="Microsoft">
//    Copyright 2012 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    [TestClass]
    public class CloudBlobContainerTest : BlobTestBase
    {
        [TestMethod]
        /// [Description("Create and delete a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerCreateAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            await container.CreateAsync();
            OperationContext operationContext = new OperationContext();
            Assert.ThrowsException<AggregateException>(
                () => container.CreateAsync(null, operationContext).AsTask().Wait(),
                "Creating already exists container should fail");
            Assert.AreEqual((int)HttpStatusCode.Conflict, operationContext.LastResult.HttpStatusCode);
            await container.DeleteAsync();
        }

        [TestMethod]
        /// [Description("Try to create a container after it is created")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerCreateIfNotExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                Assert.IsTrue(await container.CreateIfNotExistsAsync());
                Assert.IsFalse(await container.CreateIfNotExistsAsync());
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("Try to delete a non-existing container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerDeleteIfExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            Assert.IsFalse(await container.DeleteIfExistsAsync());
            await container.CreateAsync();
            Assert.IsTrue(await container.DeleteIfExistsAsync());
            Assert.IsFalse(await container.DeleteIfExistsAsync());
        }

        [TestMethod]
        /// [Description("Check a container's existence")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerExistsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                Assert.IsFalse(await container.ExistsAsync());
                await container.CreateAsync();
                Assert.IsTrue(await container.ExistsAsync());
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
            Assert.IsFalse(await container.ExistsAsync());
        }

        [TestMethod]
        /// [Description("Set container permissions")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerSetPermissionsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                BlobContainerPermissions permissions = await container.GetPermissionsAsync();
                Assert.AreEqual(BlobContainerPublicAccessType.Off, permissions.PublicAccess);
                Assert.AreEqual(0, permissions.SharedAccessPolicies.Count);

                // We do not have precision at milliseconds level. Hence, we need
                // to recreate the start DateTime to be able to compare it later.
                DateTime start = DateTime.UtcNow;
                start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second, DateTimeKind.Utc);
                DateTime expiry = start.AddMinutes(30);

                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                permissions.SharedAccessPolicies.Add("key1", new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = start,
                    SharedAccessExpiryTime = expiry,
                    Permissions = SharedAccessBlobPermissions.List,
                });
                await container.SetPermissionsAsync(permissions);
                await Task.Delay(30 * 1000);

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                permissions = await container2.GetPermissionsAsync();
                Assert.AreEqual(BlobContainerPublicAccessType.Container, permissions.PublicAccess);
                Assert.AreEqual(1, permissions.SharedAccessPolicies.Count);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.HasValue);
                Assert.AreEqual(start, permissions.SharedAccessPolicies["key1"].SharedAccessStartTime.Value.UtcDateTime);
                Assert.IsTrue(permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.HasValue);
                Assert.AreEqual(expiry, permissions.SharedAccessPolicies["key1"].SharedAccessExpiryTime.Value.UtcDateTime);
                Assert.AreEqual(SharedAccessBlobPermissions.List, permissions.SharedAccessPolicies["key1"].Permissions);
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerCreateWithMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Metadata.Add("key1", "value1");
                await container.CreateAsync();

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                await container2.FetchAttributesAsync();
                Assert.AreEqual(1, container2.Metadata.Count);
                Assert.AreEqual("value1", container2.Metadata["key1"]);

                Assert.IsTrue(container2.Properties.LastModified.Value.AddHours(1) > DateTimeOffset.Now);
                Assert.IsNotNull(container2.Properties.ETag);
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("Create a container with metadata")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerSetMetadataAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlobContainer container2 = container.ServiceClient.GetContainerReference(container.Name);
                await container2.FetchAttributesAsync();
                Assert.AreEqual(0, container2.Metadata.Count);

                container.Metadata.Add("key1", "value1");
                await container.SetMetadataAsync();

                await container2.FetchAttributesAsync();
                Assert.AreEqual(1, container2.Metadata.Count);
                Assert.AreEqual("value1", container2.Metadata["key1"]);

                ContainerResultSegment results = await container.ServiceClient.ListContainersSegmentedAsync(container.Name, ContainerListingDetails.Metadata, null, null, null, null);
                CloudBlobContainer container3 = results.Results.First();
                Assert.AreEqual(1, container3.Metadata.Count);
                Assert.AreEqual("value1", container3.Metadata["key1"]);

                container.Metadata.Clear();
                await container.SetMetadataAsync();

                await container2.FetchAttributesAsync();
                Assert.AreEqual(0, container2.Metadata.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                List<string> blobNames = await CreateBlobsAsync(container, 3, BlobType.PageBlob);

                BlobResultSegment results = await container.ListBlobsSegmentedAsync(null);
                Assert.AreEqual(blobNames.Count, results.Results.Count());
                foreach (IListBlobItem blobItem in results.Results)
                {
                    Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                    Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                }
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("List blobs")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerListBlobsSegmentedAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                List<string> blobNames = await CreateBlobsAsync(container, 3, BlobType.PageBlob);

                BlobContinuationToken token = null;
                do
                {
                    BlobResultSegment results = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, 1, token, null, null);
                    int count = 0;
                    foreach (IListBlobItem blobItem in results.Results)
                    {
                        Assert.IsInstanceOfType(blobItem, typeof(CloudPageBlob));
                        Assert.IsTrue(blobNames.Remove(((CloudPageBlob)blobItem).Name));
                        count++;
                    }
                    Assert.AreEqual(1, count);
                    token = results.ContinuationToken;
                }
                while (token != null);
                Assert.AreEqual(0, blobNames.Count);
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("Get a blob reference without knowing its type")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerGetBlobReferenceFromServerAsync()
        {
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference("bb");
                await blockBlob.PutBlockListAsync(new List<string>());

                CloudPageBlob pageBlob = container.GetPageBlobReference("pb");
                await pageBlob.CreateAsync(0);

                ICloudBlob blob1 = await container.GetBlobReferenceFromServerAsync("bb");
                Assert.IsInstanceOfType(blob1, typeof(CloudBlockBlob));

                ICloudBlob blob2 = await container.GetBlobReferenceFromServerAsync("pb");
                Assert.IsInstanceOfType(blob2, typeof(CloudPageBlob));

                ICloudBlob blob3 = await container.ServiceClient.GetBlobReferenceFromServerAsync(blockBlob.Uri);
                Assert.IsInstanceOfType(blob3, typeof(CloudBlockBlob));

                ICloudBlob blob4 = await container.ServiceClient.GetBlobReferenceFromServerAsync(pageBlob.Uri);
                Assert.IsInstanceOfType(blob4, typeof(CloudPageBlob));
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("Test conditional access on a container")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task CloudBlobContainerConditionalAccessAsync()
        {
            OperationContext operationContext = new OperationContext();
            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                await container.CreateAsync();
                await container.FetchAttributesAsync();

                string currentETag = container.Properties.ETag;
                DateTimeOffset currentModifiedTime = container.Properties.LastModified.Value;

                // ETag conditional tests
                container.Metadata["ETagConditionalName"] = "ETagConditionalValue";
                await container.SetMetadataAsync();

                await container.FetchAttributesAsync();
                string newETag = container.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");

                // LastModifiedTime tests
                currentModifiedTime = container.Properties.LastModified.Value;

                container.Metadata["DateConditionalName"] = "DateConditionalValue";

                await TestHelper.ExpectedExceptionAsync(
                    async () => await container.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(currentModifiedTime), null, operationContext),
                    operationContext,
                    "IfModifiedSince conditional on current modified time should throw",
                    HttpStatusCode.PreconditionFailed,
                    "ConditionNotMet");

                container.Metadata["DateConditionalName"] = "DateConditionalValue2";
                currentETag = container.Properties.ETag;

                DateTimeOffset pastTime = currentModifiedTime.Subtract(TimeSpan.FromMinutes(5));
                await container.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromHours(5));
                await container.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                pastTime = currentModifiedTime.Subtract(TimeSpan.FromDays(5));
                await container.SetMetadataAsync(AccessCondition.GenerateIfModifiedSinceCondition(pastTime), null, null);

                await container.FetchAttributesAsync();
                newETag = container.Properties.ETag;
                Assert.AreNotEqual(newETag, currentETag, "ETage should be modified on write metadata");
            }
            finally
            {
                container.DeleteIfExistsAsync().AsTask().Wait();
            }
        }
    }
}
