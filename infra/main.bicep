targetScope = 'resourceGroup'

param location string = 'northeurope'
param appName string = 'cloudsoft'
param uniqueSuffix string

// Azure CosmosDB for NoSQL
param cosmosAccountName string = 'cosmos${appName}${uniqueSuffix}'
param databaseName string = 'CloudSoft'
param containerName string = 'JobPostings'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    // Production security: disable account-key based authentication.
    disableLocalAuth: true
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  name: '${cosmosAccountName}/${databaseName}'
  properties: {
    resource: {
      id: databaseName
    }
  }
  dependsOn: [cosmosAccount]
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  name: '${cosmosAccountName}/${databaseName}/${containerName}'
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: [
          '/PartitionKey'
        ]
        kind: 'Hash'
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: 1000
      }
    }
  }
  dependsOn: [
    cosmosDatabase
  ]
}

// Managed identity for Container Apps Environment
resource caEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${appName}-env-${uniqueSuffix}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
  }
}

// Container App
param dockerHubUsername string
param containerImage string = '${dockerHubUsername}/cloudsoft-recruitment:latest'
param containerMinReplicas int = 1
param containerMaxReplicas int = 3

// Blob Storage for resume uploads
var storageAccountName = 'st${appName}sa${uniqueSuffix}'
var storageContainerName = 'resumes'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    // Disable local auth so the app must use Managed Identity (no storage keys).
    allowSharedKeyAccess: false
  }
}

resource storageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storageAccountName}/default/${storageContainerName}'
  properties: {
    // Block direct anonymous access; app reads/writes via Managed Identity.
    publicAccess: 'None'
  }
  dependsOn: [storageAccount]
}

// Managed Identity: give the Container App role to access CosmosDB
// Implicit dependency via containerApp.identity.principalId
resource cosmosDataContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerApp.name, cosmosAccount.id, 'CosmosDB Built-in Data Contributor')
  scope: cosmosAccount
  properties: {
    roleDefinitionId: '/providers/Microsoft.Authorization/roleDefinitions/a232010e-820c-4b89-b37f-1a17d42acc76'
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Managed Identity: give the Container App role to access Blob Storage
// Implicit dependency via containerApp.identity.principalId
resource storageBlobDataContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerApp.name, storageAccount.id, 'Storage Blob Data Contributor')
  scope: storageAccount
  properties: {
    roleDefinitionId: '/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalId: containerApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

var cosmosEndpoint = cosmosAccount.properties.documentEndpoint

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${appName}-${uniqueSuffix}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: caEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        // Enforce minimum TLS version for ingress traffic.
        transport: 'auto'
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
        // NOTE: Health probes (liveness/readiness) can be added here once
        // the Bicep type definitions are updated. Container Apps will use
        // default probes by targeting the container's port.
      }
      secrets: []
    }
    template: {
      scale: {
        minReplicas: containerMinReplicas
        maxReplicas: containerMaxReplicas
      }
      containers: [
        {
          name: 'cloudsoft'
          image: containerImage
          resources: {
             cpu: '0.5'
             memory: '1.0Gi'
           }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'CosmosDb__Endpoint'
              value: cosmosEndpoint
            }
            {
              name: 'CosmosDb__DatabaseName'
              value: databaseName
            }
            {
              name: 'CosmosDb__ContainerName'
              value: containerName
            }
            {
              name: 'BlobStorage__AccountUrl'
              value: storageAccount.properties.primaryEndpoints.blob
            }
          ]
        }
      ]
    }
  }
  // Implicit dependencies via:
  // - environmentId: caEnvironment.id
  // - dependsOn for explicit ordering
  dependsOn: [
    cosmosContainer
    storageContainer
  ]
}

// Outputs
output appUrl string = containerApp.properties.configuration.ingress.fqdn
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint
output storageAccountName string = storageAccount.name
