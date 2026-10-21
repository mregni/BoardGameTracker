import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/hooks/useAuth";
import { usePermissions } from "@/hooks/usePermissions";
import { getOidcProviders } from "@/services/queries/oidc";

export const useSingleSignOnEnabled = (): boolean => {
	const { canManageSettings } = usePermissions();
	const authEnabled = useAuth((s) => s.authStatus?.authEnabled === true);
	const { data: providers } = useQuery({ ...getOidcProviders(), enabled: authEnabled && canManageSettings });
	return providers?.some((provider) => provider.enabled) ?? false;
};
