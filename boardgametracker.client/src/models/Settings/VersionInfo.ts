export interface VersionInfo {
  currentVersion: string;
  latestVersion: string | null;
  updateAvailable: boolean;
  lastChecked: Date | null;
  errorMessage: string | null;
}
