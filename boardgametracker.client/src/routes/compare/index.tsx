import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useMemo } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import Home from "@/assets/icons/home.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { usePermissions } from "@/hooks/usePermissions";
import { getPlayers } from "@/services/queries/players";
import { CompareSummaryStats } from "./-components/CompareSummaryStats";
import { HeadToHead } from "./-components/HeadToHead";
import { PlayerSelector } from "./-components/PlayerSelector";
import { PlayerStatsSection } from "./-components/PlayerStatsSection";
import { useCompareData } from "./-hooks/useCompareData";
import { calculateOverallWinner } from "./-utils/compareUtils";

const compareSearchSchema = z.object({
	left: z.number().optional(),
	right: z.number().optional(),
});

export const Route = createFileRoute("/compare/")({
	component: RouteComponent,
	validateSearch: compareSearchSchema,
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getPlayers());
	},
});

function RouteComponent() {
	const { t } = useTranslation("compare");
	const navigate = useNavigate();
	const search = Route.useSearch();
	const { canWrite } = usePermissions();

	const { players } = useCompareData({ playerLeft: 0, playerRight: 0 });

	// Determine player IDs: URL params > first two players > 0
	const [leftPlayerId, rightPlayerId] = useMemo(() => {
		if (search.left !== undefined && search.right !== undefined) {
			return [search.left, search.right];
		}
		if (players && players.length >= 2) {
			return [players[0].id, players[1].id];
		}
		return [0, 0];
	}, [search.left, search.right, players]);

	const { compare: actualCompare, settings } = useCompareData({
		playerLeft: leftPlayerId,
		playerRight: rightPlayerId,
	});

	const handleLeftPlayerChange = useCallback(
		(playerId: number) => {
			navigate({
				to: "/compare",
				search: { left: playerId, right: rightPlayerId },
				replace: true,
			});
		},
		[navigate, rightPlayerId],
	);

	const handleRightPlayerChange = useCallback(
		(playerId: number) => {
			navigate({
				to: "/compare",
				search: { left: leftPlayerId, right: playerId },
				replace: true,
			});
		},
		[navigate, leftPlayerId],
	);

	const playerOne = useMemo(() => players?.find((p) => p.id === leftPlayerId), [players, leftPlayerId]);

	const playerTwo = useMemo(() => players?.find((p) => p.id === rightPlayerId), [players, rightPlayerId]);

	if (players?.length < 2) {
		return (
			<BgtEmptyPage
				header={t("title")}
				icon={Users}
				title={t("empty.insufficient-players.title")}
				description={t("empty.insufficient-players.description")}
				action={canWrite ? { label: t("empty.button"), onClick: () => navigate({ to: "/players" }) } : undefined}
			/>
		);
	}

	return (
		<BgtPage>
			<BgtPageHeader header={t("title")} icon={Home} />
			<BgtPageContent isLoading={!players} data={{ players }} centered={players?.length < 2}>
				{({ players }) => {
					if (!actualCompare || !settings || !playerOne || !playerTwo) {
						return null;
					}

					const overallWinner = calculateOverallWinner(actualCompare, settings.uiLanguage);

					return (
						<div className="max-w-7xl mx-auto">
							<div className="flex flex-row items-center justify-center gap-3 md:gap-16 mb-12">
								<PlayerSelector
									player={playerOne}
									players={players}
									isWinner={overallWinner === "playerOne"}
									onPlayerChange={handleLeftPlayerChange}
								/>

								<img src="/images/common/vs.png" alt="divider" className="w-10 md:w-18" />

								<PlayerSelector
									player={playerTwo}
									players={players}
									isWinner={overallWinner === "playerTwo"}
									onPlayerChange={handleRightPlayerChange}
								/>
							</div>

							<CompareSummaryStats compare={actualCompare} />

							<div className="grid grid-cols-1 md:grid-cols-2 gap-3 xl:gap-6 mb-12 ">
								<PlayerStatsSection
									player={playerOne}
									color="red"
									playerKey="playerOne"
									opponentKey="playerTwo"
									compare={actualCompare}
									uiLanguage={settings.uiLanguage}
								/>
								<PlayerStatsSection
									player={playerTwo}
									color="blue"
									playerKey="playerTwo"
									opponentKey="playerOne"
									compare={actualCompare}
									uiLanguage={settings.uiLanguage}
								/>
							</div>

							<HeadToHead
								playerOne={playerOne}
								playerTwo={playerTwo}
								compare={actualCompare}
								dateFormat={settings.dateFormat}
								uiLanguage={settings.uiLanguage}
							/>
						</div>
					);
				}}
			</BgtPageContent>
		</BgtPage>
	);
}
