import { getSettingsCall, getEnvironmentCall, getLanguagesCall, getVersionInfoCall } from '../settingsService';

import { createListQuery, createSingletonQuery } from './queryFactory';

import { QUERY_KEYS } from '@/models';

export const getSettings = createSingletonQuery(QUERY_KEYS.settings, getSettingsCall);

export const getEnvironment = createSingletonQuery(QUERY_KEYS.environment, getEnvironmentCall);

export const getLanguages = createListQuery(QUERY_KEYS.languages, getLanguagesCall);

export const getVersionInfo = createSingletonQuery(QUERY_KEYS.versionInfo, getVersionInfoCall);
