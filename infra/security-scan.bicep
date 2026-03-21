@description('The container image to deploy (e.g., ghcr.io/mregni/boardgametracker@sha256:...)')
param imageName string

@description('PostgreSQL admin password')
@secure()
param dbPassword string

@description('JWT signing secret')
@secure()
param jwtSecret string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Unique suffix for resource names')
param suffix string = uniqueString(resourceGroup().id)

// ── Log Analytics Workspace ──

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'law-bgt-sec-${suffix}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 7
  }
}

// ── Container Apps Environment ──

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-bgt-sec-${suffix}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ── PostgreSQL Container App (internal only) ──

resource postgresApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'postgres-bgt-sec'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      secrets: [
        { name: 'db-password', value: dbPassword }
      ]
      ingress: {
        external: false
        targetPort: 5432
        transport: 'tcp'
        exposedPort: 5432
      }
    }
    template: {
      containers: [
        {
          name: 'postgres'
          image: 'docker.io/library/postgres:16-alpine'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'POSTGRES_DB', value: 'boardgametracker' }
            { name: 'POSTGRES_USER', value: 'bgtuser' }
            { name: 'POSTGRES_PASSWORD', secretRef: 'db-password' }
          ]
          probes: [
            {
              type: 'Readiness'
              tcpSocket: {
                port: 5432
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// ── BGT API Container App (public HTTP) ──

resource bgtApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'bgt-api-sec'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      secrets: [
        { name: 'db-password', value: dbPassword }
        { name: 'jwt-secret', value: jwtSecret }
      ]
      ingress: {
        external: true
        targetPort: 5444
        transport: 'http'
        allowInsecure: false
      }
      registries: []
    }
    template: {
      containers: [
        {
          name: 'bgt-api'
          image: imageName
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'production' }
            { name: 'ASPNETCORE_URLS', value: 'http://*:5444' }
            { name: 'DB_HOST', value: postgresApp.properties.configuration.ingress.fqdn }
            { name: 'DB_PORT', value: '5432' }
            { name: 'DB_USER', value: 'bgtuser' }
            { name: 'DB_PASSWORD', secretRef: 'db-password' }
            { name: 'DB_NAME', value: 'boardgametracker' }
            { name: 'JWT_SECRET', secretRef: 'jwt-secret' }
            { name: 'AUTH_ENABLED', value: 'true' }
            { name: 'TZ', value: 'UTC' }
          ]
          probes: [
            {
              type: 'Readiness'
              httpGet: {
                path: '/api/health'
                port: 5444
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/api/health'
                port: 5444
              }
              initialDelaySeconds: 30
              periodSeconds: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// ── Outputs ──

output appFqdn string = bgtApp.properties.configuration.ingress.fqdn
output appUrl string = 'https://${bgtApp.properties.configuration.ingress.fqdn}'
