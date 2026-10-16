import { noop } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useCallback, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import LeftRightArrow from "@/assets/icons/left-right-arrow.svg?react";
import TrendUp from "@/assets/icons/trend-up.svg?react";
import Users from "@/assets/icons/users.svg?react";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtEmptyPage } from "@/components/BgtLayout/BgtEmptyPage";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { usePermissions } from "@/hooks/usePermissions";
import type { CompareResult, CompareSide, Player } from "@/models";
import { getPlayers } from "@/services/queries/players";
import { BestWorstCards } from "./-components/BestWorstCards";
import { Highlights } from "./-components/Highlights";
import { OverallTable } from "./-components/OverallTable";
import { PerGameCard } from "./-components/PerGameCard";
import { PlayerPicker } from "./-components/PlayerPicker";
import { RecentTogether } from "./-components/RecentTogether";
import { RecordCard } from "./-components/RecordCard";
import { TogetherTiles } from "./-components/TogetherTiles";
import { useCompareData } from "./-hooks/useCompareData";

const compareSearchSchema = z.object({
	left: z.number().optional(),
	right: z.number().optional(),
});

export const Route = createFileRoute("/compare/")({
	component: RouteComponent,
	validateSearch: compareSearchSchema,
	loader: ({ context: { queryClient } }) => {
		queryClient.query(getPlayers()).catch(noop);
	},
});

function RouteComponent() {
	const { t } = useTranslation("compare");
	const navigate = useNavigate();
	const search = Route.useSearch();
	const { canWrite } = usePermissions();
	const { percent } = useNumberFormat();

	const { players, leftId, rightId, shownLeftId, shownRightId, compare, settings, isLoading, isUpdating } =
		useCompareData({
			left: search.left,
			right: search.right,
		});

	const goTo = useCallback(
		(left: number, right: number) => navigate({ to: "/compare", search: { left, right }, replace: true }),
		[navigate],
	);

	useEffect(() => {
		if (leftId === 0 || rightId === 0) return;
		if (search.left === leftId && search.right === rightId) return;
		goTo(leftId, rightId);
	}, [goTo, leftId, rightId, search.left, search.right]);

	const playerOne = players.find((player) => player.id === leftId);
	const playerTwo = players.find((player) => player.id === rightId);

	if (!isLoading && players.length < 2) {
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

	const shownOne = players.find((player) => player.id === shownLeftId);
	const shownTwo = players.find((player) => player.id === shownRightId);

	const summary = (data: CompareResult, player: Player) => {
		const side: CompareSide | null =
			player.id === shownLeftId ? "playerOne" : player.id === shownRightId ? "playerTwo" : null;
		return side
			? t("player-summary", { count: data.sessionCounts[side], rate: percent(data.winPercentage[side]) })
			: "";
	};

	return (
		<BgtPage>
			<BgtPageHeader header={t("title")} icon={TrendUp} />
			<BgtPageContent isLoading={isLoading} data={{ compare, settings, playerOne, playerTwo, shownOne, shownTwo }}>
				{({ compare, settings, playerOne, playerTwo, shownOne, shownTwo }) => (
					<div className="mx-auto flex w-full max-w-5xl flex-col gap-4">
						<div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-1.5 sm:gap-3">
							<PlayerPicker
								side="playerOne"
								player={playerOne}
								players={players}
								excludeId={playerTwo.id}
								summary={summary(compare, playerOne)}
								onPlayerChange={(id) => goTo(id, playerTwo.id)}
							/>
							<div className="flex flex-col items-center gap-1">
								<span className="text-lg font-bold tracking-widest text-white/60">{t("vs")}</span>
								<BgtIconButton
									intent="subtile"
									className="border border-card-border text-white/60 hover:text-white"
									icon={<LeftRightArrow className="size-4" />}
									aria-label={t("swap")}
									onClick={() => goTo(playerTwo.id, playerOne.id)}
								/>
								<span
									role="status"
									aria-label={isUpdating ? t("updating") : undefined}
									className={cx(
										"size-3 rounded-full border-2 border-white/20 border-t-white/70 transition-opacity motion-safe:animate-spin",
										isUpdating ? "opacity-100" : "opacity-0",
									)}
								/>
							</div>
							<PlayerPicker
								side="playerTwo"
								player={playerTwo}
								players={players}
								excludeId={playerOne.id}
								summary={summary(compare, playerTwo)}
								onPlayerChange={(id) => goTo(playerOne.id, id)}
							/>
						</div>
						<div
							data-testid="compare-content"
							aria-busy={isUpdating}
							className={cx(
								"flex flex-col gap-4 motion-safe:transition-opacity motion-safe:duration-300",
								isUpdating && "opacity-60",
							)}
						>
							<Together
								compare={compare}
								playerOne={shownOne}
								playerTwo={shownTwo}
								dateFormat={settings.dateFormat}
								uiLanguage={settings.uiLanguage}
								canOpen={canWrite}
							/>
							<OverallTable compare={compare} playerOne={shownOne} playerTwo={shownTwo} />
							<Highlights compare={compare} dateFormat={settings.dateFormat} uiLanguage={settings.uiLanguage} />
						</div>
					</div>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}

interface TogetherProps {
	compare: CompareResult;
	playerOne: Player;
	playerTwo: Player;
	dateFormat: string;
	uiLanguage: string;
	canOpen: boolean;
}

const Together = ({ compare, playerOne, playerTwo, dateFormat, uiLanguage, canOpen }: TogetherProps) => {
	const { t } = useTranslation("compare");

	if (compare.totalSessionsTogether === 0) {
		return (
			<section className="flex flex-col items-center gap-1 rounded-xl border border-card-border bg-card p-6 text-center">
				<h2 className="font-bold">{t("no-games-together.title")}</h2>
				<p className="text-sm text-white/60">
					{t("no-games-together.description", { playerOne: playerOne.name, playerTwo: playerTwo.name })}
				</p>
			</section>
		);
	}

	return (
		<>
			<RecordCard compare={compare} playerOne={playerOne} playerTwo={playerTwo} />
			<TogetherTiles
				compare={compare}
				playerOne={playerOne}
				playerTwo={playerTwo}
				dateFormat={dateFormat}
				uiLanguage={uiLanguage}
			/>
			<PerGameCard
				games={compare.games}
				playerOne={playerOne}
				playerTwo={playerTwo}
				dateFormat={dateFormat}
				uiLanguage={uiLanguage}
			/>
			<BestWorstCards compare={compare} playerOne={playerOne} playerTwo={playerTwo} />
			<RecentTogether
				sessions={compare.recentSessions}
				playerOne={playerOne}
				playerTwo={playerTwo}
				dateFormat={dateFormat}
				uiLanguage={uiLanguage}
				canOpen={canOpen}
			/>
		</>
	);
};
