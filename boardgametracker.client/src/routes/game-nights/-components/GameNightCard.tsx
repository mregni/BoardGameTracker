import { Link, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CircleHelp from "@/assets/icons/circle-help.svg?react";
import Clock from "@/assets/icons/clock.svg?react";
import MapPin from "@/assets/icons/map-pin.svg?react";
import User from "@/assets/icons/user.svg?react";
import X from "@/assets/icons/x.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChip } from "@/components/GameNight/AvatarChip";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
import { type GameNight, GameNightRsvpState, type Player, type Settings } from "@/models";
import { toDisplayTime } from "@/utils/dateUtils";
import { relativeDay } from "@/utils/relativeDay";
import { sortRsvps } from "@/utils/sortRsvps";
import { useInviteLink } from "../-hooks/useInviteLink";
import { EmailInvitesButton } from "./EmailInvitesButton";
import { GameNightActions } from "./GameNightActions";
import { RsvpAvatarStack } from "./RsvpAvatarStack";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	players: Player[];
	canWrite: boolean;
	highlight?: boolean;
	live?: boolean;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
	onManageRsvps: (gameNight: GameNight) => void;
}

export const GameNightCard = (props: Props) => {
	const {
		gameNight,
		settings,
		players,
		canWrite,
		highlight = false,
		live = false,
		onEdit,
		onDelete,
		onManageRsvps,
	} = props;
	const { t } = useTranslation("game-nights");
	const navigate = useNavigate();

	const rsvps = sortRsvps(gameNight.invitedPlayers, gameNight.hostId);
	const namesIn = (state: GameNightRsvpState) =>
		rsvps
			.filter((rsvp) => rsvp.state === state)
			.map((rsvp) => rsvp.player.name)
			.join(", ");
	const answers = [
		{ state: GameNightRsvpState.Pending, key: "card.waiting", icon: Clock, tone: "font-medium text-white" },
		{ state: GameNightRsvpState.Maybe, key: "card.maybe-names", icon: CircleHelp, tone: "text-gray-300" },
		{ state: GameNightRsvpState.Declined, key: "card.declined-names", icon: X, tone: "text-gray-300" },
	]
		.map((answer) => ({ ...answer, names: namesIn(answer.state) }))
		.filter((answer) => answer.names !== "");
	const showManageRsvps = canWrite && !live;
	const { copied, copy } = useInviteLink(gameNight);
	const startTime = toDisplayTime(gameNight.startDate, settings.timeFormat, settings.uiLanguage);
	const runningGames = gameNight.summary.runningGames;

	return (
		<article
			data-testid="game-night-card"
			className={cx(
				"relative grid grid-cols-[3.5rem_minmax(0,1fr)_auto] gap-x-3 gap-y-4 rounded-lg border transition-colors has-[a:focus-visible]:ring-2 has-[a:focus-visible]:ring-white/80",
				highlight
					? "border-primary/50 bg-primary/15 p-4 md:grid-cols-[6rem_minmax(0,1fr)_auto] md:gap-x-6 md:p-6 has-[a:hover]:border-primary"
					: "border-primary/20 bg-primary/10 p-4 md:grid-cols-[4.75rem_minmax(0,1fr)_auto] md:gap-x-4 has-[a:hover]:border-primary/60",
			)}
		>
			<GameNightDateBox
				date={gameNight.startDate}
				uiLanguage={settings.uiLanguage}
				variant={highlight ? "highlight" : "soft"}
				className={cx("self-start", highlight && "md:py-3")}
			/>
			<div className="flex min-w-0 flex-col items-start gap-1.5 self-center">
				<span
					className={cx(
						"rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap tabular-nums first-letter:uppercase",
						highlight ? "bg-primary/25 text-indigo-200" : "bg-white/10 text-gray-300",
					)}
				>
					{live ? (
						<span className="inline-flex items-center gap-2">
							<span className="relative flex size-2" aria-hidden="true">
								<span className="absolute inline-flex size-full animate-ping rounded-full bg-green-400 opacity-75 motion-reduce:animate-none" />
								<span className="relative inline-flex size-2 rounded-full bg-green-400" />
							</span>
							{t("card.live-since", { time: startTime })}
						</span>
					) : (
						t("card.when", { when: relativeDay(gameNight.startDate, settings.uiLanguage), time: startTime })
					)}
				</span>
				<BgtHeading size="5" className={cx("wrap-anywhere", highlight && "md:text-[28px]/[36px]")}>
					<Link
						to="/game-nights/$gameNightId"
						params={{ gameNightId: gameNight.id }}
						className="after:absolute after:inset-0 after:rounded-lg hover:text-primary-text focus-visible:outline-none"
					>
						{gameNight.title}
					</Link>
				</BgtHeading>
				<div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-gray-400">
					<span className="inline-flex items-center gap-1.5">
						<MapPin className="size-4 shrink-0" aria-hidden="true" />
						{gameNight.location.name}
					</span>
					<span className="inline-flex items-center gap-1.5">
						<User className="size-4 shrink-0" aria-hidden="true" />
						{t("card.host", { host: gameNight.host.name })}
					</span>
				</div>
			</div>
			<div className="relative z-20 self-start">
				{canWrite && (
					<GameNightActions
						gameNight={gameNight}
						invitable={!live}
						copyInMenu={!highlight}
						onEdit={onEdit}
						onDelete={onDelete}
					/>
				)}
			</div>

			<div className="col-span-3 flex min-w-0 flex-col gap-3 md:col-span-2 md:col-start-2">
				{live && runningGames > 0 && (
					<BgtText size="3" weight="bold">
						{t("card.running", { count: runningGames })}
					</BgtText>
				)}
				<RsvpProgress rsvps={gameNight.invitedPlayers} compact />
				<RsvpAvatarStack rsvps={rsvps} hostId={gameNight.hostId} linkPlayers={false} />
				{((!live && answers.length > 0) || canWrite) && (
					<div className="flex flex-wrap items-end justify-between gap-3">
						{!live && answers.length > 0 && (
							<ul className="flex min-w-0 flex-col gap-1 text-sm" data-testid="rsvp-names">
								{answers.map(({ state, key, icon: Icon, tone, names }) => (
									<li key={state} className={cx("flex items-start gap-2", tone)}>
										<Icon className="mt-0.5 size-4 shrink-0 text-gray-400" aria-hidden="true" />
										<span className="min-w-0 wrap-anywhere">{t(key, { names })}</span>
									</li>
								))}
							</ul>
						)}
						{canWrite && (
							<div className="relative z-10 ml-auto flex flex-wrap items-center gap-2">
								{showManageRsvps && (
									<BgtButton variant="cancel" size="2" onClick={() => onManageRsvps(gameNight)}>
										{t("card.manage-rsvps")}
									</BgtButton>
								)}
								{!live && <EmailInvitesButton gameNight={gameNight} players={players} />}
								{highlight && !live && (
									<BgtButton variant="primary" size="2" onClick={copy} disabled={copied}>
										{copied ? t("card.copied") : t("card.copy-link")}
									</BgtButton>
								)}
								{live && (
									<BgtButton
										variant="primary"
										size="2"
										onClick={() =>
											navigate({
												to: "/game-nights/$gameNightId",
												params: { gameNightId: gameNight.id },
												search: { start: true },
											})
										}
									>
										{t("play.start.button")}
									</BgtButton>
								)}
							</div>
						)}
					</div>
				)}

				{gameNight.suggestedGames.length > 0 && (
					<div className="flex flex-wrap items-center gap-2">
						<BgtText size="2" color="gray">
							{t("card.suggested-games")}
						</BgtText>
						{gameNight.suggestedGames.map((game) => (
							<AvatarChip key={game.id} title={game.title} image={game.image} />
						))}
					</div>
				)}

				{gameNight.notes && (
					<BgtText size="2" color="gray" className="line-clamp-2 border-l border-white/15 pl-3">
						{gameNight.notes}
					</BgtText>
				)}
			</div>
		</article>
	);
};
