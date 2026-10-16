import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
	type IntegrationKind,
	type IntegrationStatus,
	isIntegrationFailure,
} from "@/models/Settings/IntegrationStatus";
import { testIntegrationCall } from "@/services/integrationService";
import { getIntegrationStatuses } from "@/services/queries/settings";

const mergeStatus = (current: IntegrationStatus[] | undefined, next: IntegrationStatus): IntegrationStatus[] => [
	...(current ?? []).filter((status) => status.integration !== next.integration),
	next,
];

export const useIntegrationStatus = (enabled = true) => {
	const queryClient = useQueryClient();
	const { t } = useTranslation("settings");
	const query = useQuery({ ...getIntegrationStatuses(), enabled });

	const testMutation = useMutation({
		mutationFn: testIntegrationCall,
		onSuccess: (result) => {
			queryClient.setQueryData<IntegrationStatus[]>(getIntegrationStatuses().queryKey, (current) =>
				mergeStatus(current, result),
			);
		},
		onError: () => toast.error(t("integrations.test-failed")),
	});

	const statusFor = (integration: IntegrationKind) => query.data?.find((status) => status.integration === integration);

	return {
		statusFor,
		isChecking: query.isFetching,
		testIntegration: testMutation.mutate,
		testingIntegration: testMutation.isPending ? testMutation.variables : undefined,
	};
};

export const useRefreshIntegrationStatus = () => {
	const queryClient = useQueryClient();
	const { t } = useTranslation("settings");

	return useCallback(async () => {
		try {
			const statuses = await queryClient.fetchQuery({ ...getIntegrationStatuses(), staleTime: 0 });
			for (const status of statuses.filter(isIntegrationFailure)) {
				toast.error(t("integrations.save-failed", { name: t(`integrations.name.${status.integration}`) }), {
					description: t(`integrations.reason.${status.messageKey}`),
				});
			}
		} catch {
			toast.error(t("integrations.test-failed"));
		}
	}, [queryClient, t]);
};
