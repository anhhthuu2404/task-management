import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44399/', // <-- Sửa ở đây
  redirectUri: baseUrl,
  clientId: 'TaskManagement_App',
  responseType: 'code',
  scope: 'offline_access TaskManagement',
  requireHttps: true,
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'TaskManagement',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44399', // <-- Sửa ở đây
      rootNamespace: 'TaskManagement',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
  remoteEnv: {
    url: '/getEnvConfig',
    mergeStrategy: 'deepmerge'
  }
} as Environment;