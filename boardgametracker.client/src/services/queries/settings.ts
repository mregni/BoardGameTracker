import { QUERY_KEYS } from "@/models";
import { getIntegrationStatusesCall } from "../integrationService";
import { getEnvironmentCall, getLanguagesCall, getSettingsCall, getVersionInfoCall } from "../settingsService";
import { createListQuery, createSingletonQuery } from "./queryFactory";

export const getSettings = createSingletonQuery(QUERY_KEYS.settings, getSettingsCall);

export const getEnvironment = createSingletonQuery(QUERY_KEYS.environment, getEnvironmentCall);

export const getLanguages = createListQuery(QUERY_KEYS.languages, getLanguagesCall);

export const getVersionInfo = createSingletonQuery(QUERY_KEYS.versionInfo, getVersionInfoCall);

export const getIntegrationStatuses = createListQuery(QUERY_KEYS.integrationStatus, getIntegrationStatusesCall);
