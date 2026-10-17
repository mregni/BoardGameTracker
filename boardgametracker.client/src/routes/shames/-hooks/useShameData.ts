import { keepPreviousData, useMutation, useQueries } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useQueryInvalidator } from "@/hooks/useQueryInvalidator";
import type { Shame, ShameGameNight } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { addSuggestedGameCall, removeSuggestedGameCall } from "@/services/gameNightService";
import { getShameStatistics, getShames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { DEFAULT_DATE_FORMAT, toDisplay } from "@/utils/dateUtils";

interface PlanArgs {
	shame: Shame;
	night: ShameGameNight;
}

export const useShameData = (months?: number) => {
	const { i18n } = useTranslation();
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();

	const [shameQuery, shameStatisticsQuery, settingsQuery] = useQueries({
		queries: [
			{ ...getShames(months), placeholderData: keepPreviousData },
			{ ...getShameStatistics(months), placeholderData: keepPreviousData },
			getSettings(),
		],
	});

	const refresh = () => Promise.all([invalidator.invalidateShames(), invalidator.invalidateGameNights()]);

	const undoMutation = useMutation({
		mutationFn: ({ shame, night }: PlanArgs) => removeSuggestedGameCall(night.id, shame.id),
		onSuccess: (_, { shame }) => {
			successToast("shames:plan.removed", undefined, { game: shame.title });
			return refresh();
		},
		onError: () => errorToast("shames:plan.undo-failed"),
	});

	const planMutation = useMutation({
		mutationFn: ({ shame, night }: PlanArgs) => addSuggestedGameCall(night.id, shame.id),
		onSuccess: (_, args) => {
			successToast(
				"shames:plan.added",
				{ label: "shames:plan.undo", onClick: () => undoMutation.mutate(args) },
				{
					game: args.shame.title,
					night: args.night.title,
					date: toDisplay(args.night.startDate, settingsQuery.data?.dateFormat ?? DEFAULT_DATE_FORMAT, i18n.language),
				},
			);
			return refresh();
		},
		onError: () => errorToast("shames:plan.failed"),
	});

	return {
		shames: shameQuery.data ?? [],
		statistics: shameStatisticsQuery.data,
		settings: settingsQuery.data,
		isLoading: shameQuery.isLoading || shameStatisticsQuery.isLoading || settingsQuery.isLoading,
		plan: (shame: Shame, night: ShameGameNight) => planMutation.mutate({ shame, night }),
		planningId: planMutation.isPending ? planMutation.variables?.shame.id : undefined,
	};
};
