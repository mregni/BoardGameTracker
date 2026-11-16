import { useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { PlannedGameList } from "@/components/GameNight/PlannedGameList";
import { RsvpGroups } from "@/components/GameNight/RsvpGroups";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import { useStorageData } from "@/hooks/useStorage";
import { type Game, type GameNight, GameNightRsvpState } from "@/models";
import { canStore, placeLabel, placeOf } from "@/utils/storage";
import { useInviteLink } from "../-hooks/useInviteLink";
import { NightSectionTitle } from "./NightSectionTitle";

interface Props {
	gameNight: GameNight;
	games: Game[];
	canWrite: boolean;
	showGames: boolean;
	onManageRsvps: () => void;
}

const chip = "inline-flex shrink-0 items-center whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-semibold";

const bringAlong = (game: Game, locationId: number) => {
	if (!canStore(game.state)) return null;
	if (game.isLoaned) return "loaned";
	const place = placeOf(game);
	if (!place) return "unplaced";
	return place.locationId === locationId ? "here" : "bring";
};

export const NightPlanPanel = ({ gameNight, games, canWrite, showGames, onManageRsvps }: Props) => {
	const { t } = useTranslation(["game-nights", "storage"]);
	const navigate = useNavigate();
	const { copied, copy } = useInviteLink(gameNight);
	const { nodes, locations } = useStorageData();
	const waiting = gameNight.invitedPlayers.filter((rsvp) => rsvp.state === GameNightRsvpState.Pending).length;
	const plannedGames = gameNight.suggestedGames.map((game) => games.find((x) => x.id === game.id) ?? game);
	const toBring = plannedGames.filter((game) => bringAlong(game, gameNight.locationId) === "bring").length;

	const storageLine = (game: Game) => {
		const status = bringAlong(game, gameNight.locationId);
		if (!status) return null;
		const place = placeOf(game);
		return (
			<span className="flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-white/50">
				<span className="wrap-anywhere">
					{place ? placeLabel(place, nodes, locations, t) : t("storage:place.none")}
				</span>
				{status === "loaned" && <span className={cx(chip, "bg-white/10 text-white/70")}>{t("planned.loaned")}</span>}
				{status === "bring" && <span className={cx(chip, "bg-warn/15 text-warn")}>{t("planned.bring-along")}</span>}
				{status === "here" && (
					<span className={cx(chip, "bg-ok/15 text-ok")}>
						{t("planned.at-location", { location: gameNight.location.name })}
					</span>
				)}
			</span>
		);
	};

	return (
		<div
			className={cx("grid gap-4 xl:gap-6", showGames && "lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]")}
			data-testid="night-plan"
		>
			<section className="flex flex-col gap-2" aria-labelledby="night-rsvps">
				<NightSectionTitle id="night-rsvps">{t("plan.rsvps")}</NightSectionTitle>
				<BgtCard className="gap-3">
					<RsvpProgress rsvps={gameNight.invitedPlayers} />
					<RsvpGroups gameNight={gameNight} onOpenPlayer={(id) => navigate({ to: `/players/${id}` })} />
					{canWrite && (
						<div className="flex flex-col gap-2 border-t border-white/10 pt-3">
							{waiting > 0 && (
								<BgtText size="2" color="gray">
									{t("plan.waiting", { count: waiting })}
								</BgtText>
							)}
							<div className="flex flex-wrap gap-2">
								<BgtButton variant={waiting > 0 ? "primary" : "cancel"} size="1" onClick={copy} disabled={copied}>
									{copied ? t("card.copied") : t("card.copy-link")}
								</BgtButton>
								<BgtButton variant="cancel" size="1" onClick={onManageRsvps}>
									{t("card.manage-rsvps")}
								</BgtButton>
							</div>
						</div>
					)}
				</BgtCard>
			</section>

			{showGames && (
				<section className="flex flex-col gap-2" aria-labelledby="planned-games">
					<div className="flex items-center gap-3">
						<div className="min-w-0 flex-1">
							<NightSectionTitle id="planned-games">{t("planned.title")}</NightSectionTitle>
						</div>
						{toBring > 0 && (
							<span className={cx(chip, "bg-warn/15 text-warn")}>{t("planned.to-bring", { count: toBring })}</span>
						)}
					</div>
					{plannedGames.length === 0 ? (
						<BgtText size="2" color="gray">
							{t("plan.no-games")}
						</BgtText>
					) : (
						<PlannedGameList
							games={plannedGames}
							onOpenGame={(id) => navigate({ to: `/games/${id}` })}
							details={storageLine}
						/>
					)}
				</section>
			)}
		</div>
	);
};
