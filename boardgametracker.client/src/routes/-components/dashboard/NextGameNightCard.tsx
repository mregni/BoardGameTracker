import { Link } from "@tanstack/react-router";
import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import type { DashboardGameNight } from "@/models";
import { relativeDay } from "@/utils/relativeDay";
import { cardLinkClasses, dashboardLinkClasses, pillVariants } from "./dashboardStyles";

interface Props {
	gameNight: DashboardGameNight | null;
	plannedGameNights: number;
	uiLanguage: string;
	timeFormat: string;
	canWrite: boolean;
}

export const NextGameNightCard = (props: Props) => {
	const { gameNight, plannedGameNights, uiLanguage, timeFormat, canWrite } = props;
	const { t } = useTranslation("dashboard");

	if (gameNight === null) {
		return (
			<section
				aria-label={t("night.label")}
				className="flex flex-wrap items-center gap-x-2 gap-y-1 rounded-lg border border-dashed border-primary/40 bg-primary/5 px-4 py-3 text-sm"
			>
				<Calendar className="size-4 text-primary-text" aria-hidden="true" />
				<span className="text-white/70">{t("night.none")}</span>
				{canWrite && (
					<>
						<span aria-hidden="true" className="text-white/40">
							·
						</span>
						<Link to="/game-nights" className={cardLinkClasses}>
							{t("night.plan")}
						</Link>
					</>
				)}
			</section>
		);
	}

	const start = new Date(gameNight.startDate);
	const morePlanned = Math.max(0, plannedGameNights - 1);

	return (
		<section
			aria-label={t("night.label")}
			className="rounded-lg border border-primary bg-primary/15 p-4"
			data-testid="next-game-night"
		>
			<div className="grid grid-cols-[3.5rem_minmax(0,1fr)] items-center gap-x-4 gap-y-3 md:grid-cols-[3.5rem_minmax(0,1fr)_auto]">
				<GameNightDateBox date={start} uiLanguage={uiLanguage} variant="highlight" className="self-start" />
				<div className="flex min-w-0 flex-col gap-1.5">
					<span className="text-xs text-white/60 first-letter:uppercase">
						{t("night.when", { when: relativeDay(start, uiLanguage) })}
					</span>
					<h2 className="truncate text-lg font-bold text-white" title={gameNight.title}>
						{gameNight.title}
					</h2>
					<div className="flex flex-wrap items-center gap-x-2.5 gap-y-1 text-sm">
						<span className="text-white/60">
							{t("night.details", {
								time: format(start, timeFormat),
								location: gameNight.locationName,
								host: gameNight.hostName,
							})}
						</span>
						<span className={pillVariants({ tone: "ok" })}>
							{t("night.coming", { count: gameNight.acceptedCount })}
						</span>
						{gameNight.pendingCount > 0 && (
							<span className={pillVariants({ tone: "neutral" })}>
								{t("night.pending", { count: gameNight.pendingCount })}
							</span>
						)}
						{gameNight.declinedCount > 0 && (
							<span className={pillVariants({ tone: "bad" })}>
								{t("night.declined", { count: gameNight.declinedCount })}
							</span>
						)}
					</div>
					{gameNight.suggestedGames.length > 0 && (
						<div className="flex flex-wrap items-center gap-1.5 text-sm">
							<span className="text-white/60">{t("night.suggested")}</span>
							{gameNight.suggestedGames.map((game) => (
								<Link key={game.id} to="/games/$gameId" params={{ gameId: game.id }} className="hover:opacity-80">
									<AvatarChip title={game.title} image={game.image} />
								</Link>
							))}
						</div>
					)}
				</div>
				<div className="col-span-2 flex flex-col items-stretch gap-2 md:col-span-1 md:items-end">
					<Link
						to="/game-nights/$gameNightId"
						params={{ gameNightId: gameNight.id }}
						className={dashboardLinkClasses({ variant: "primary" })}
					>
						{t("night.open")}
					</Link>
					{morePlanned > 0 && (
						<Link to="/game-nights" className={`${cardLinkClasses} justify-center`}>
							{t("night.more", { count: morePlanned })}
						</Link>
					)}
				</div>
			</div>
		</section>
	);
};
