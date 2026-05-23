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

var cosmosConnectionString = 'AccountEndpoint=${cosmosAccount.properties.documentEndpoint};AccountKey=${cosmosAccount.listKeys().primaryMasterKey};'

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${appName}-${uniqueSuffix}'
  location: location
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
      }
      secrets: [
        {
          name: 'cosmos-connection-string'
          value: cosmosConnectionString
        }
      ]
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
              name: 'ConnectionStrings__CosmosDb'
              secretRef: 'cosmos-connection-string'
            }
            {
              name: 'CosmosDb__DatabaseName'
              value: databaseName
            }
            {
              name: 'CosmosDb__ContainerName'
              value: containerName
            }
          ]
        }
      ]
    }
  }
  dependsOn: [cosmosContainer]
}

// Outputs
output appUrl string = containerApp.properties.configuration.ingress.fqdn
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint
