import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import SearchIcon from "@/assets/icons/magnifying-glass.svg?react";
import PlusIcon from "@/assets/icons/plus.svg?react";
import Cross from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtSimpleInputField } from "@/components/BgtForm/BgtSimpleInputField";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";
import {
	type GameNight,
	GameNightRsvpState,
	type GameNightRsvps,
	type ModalProps,
	type Player,
	type Settings,
	type UpdateGameNight,
	type UpdateGameNightRsvp,
} from "@/models";
import { toDisplayDateTime } from "@/utils/dateUtils";
import { RsvpProgress } from "../-components/RsvpProgress";
import { useInviteLink } from "../-hooks/useInviteLink";

interface Props extends ModalProps {
	gameNight: GameNight | null;
	players: Player[];
	settings: Settings;
	onUpdateRsvp: (rsvp: UpdateGameNightRsvp) => Promise<unknown>;
	onUpdateGameNight: (gameNight: UpdateGameNight) => Promise<unknown>;
	isLoading: boolean;
}

const MAX_SUGGESTIONS = 8;

const options = [
	{ state: GameNightRsvpState.Accepted, label: "rsvp.coming", active: "bg-green-500/20 text-green-300" },
	{ state: GameNightRsvpState.Pending, label: "common:pending", active: "bg-amber-500/20 text-amber-300" },
	{ state: GameNightRsvpState.Declined, label: "common:declined", active: "bg-red-500/20 text-red-300" },
] as const;

export const ManageRSVPsModal = (props: Props) => {
	const { gameNight, ...rest } = props;

	if (!gameNight) return null;

	return <ManageRSVPsDialog gameNight={gameNight} {...rest} />;
};

const ManageRSVPsDialog = (props: Props & { gameNight: GameNight }) => {
	const { open, close, gameNight, players, settings, onUpdateRsvp, onUpdateGameNight, isLoading } = props;
	const { t } = useTranslation(["game-nights", "common"]);
	const { link, copied, copy } = useInviteLink(gameNight);
	const [query, setQuery] = useState("");

	const rsvps = useMemo(
		() =>
			[...gameNight.invitedPlayers].sort((a, b) => {
				if (a.playerId === gameNight.hostId) return -1;
				if (b.playerId === gameNight.hostId) return 1;
				return a.player.name.localeCompare(b.player.name);
			}),
		[gameNight.invitedPlayers, gameNight.hostId],
	);

	const suggestions = useMemo(() => {
		const invited = new Set(gameNight.invitedPlayers.map((rsvp) => rsvp.playerId));
		const search = query.trim().toLowerCase();
		return players
			.filter((player) => !invited.has(player.id))
			.filter((player) => search === "" || player.name.toLowerCase().includes(search))
			.slice(0, MAX_SUGGESTIONS);
	}, [players, gameNight.invitedPlayers, query]);

	const hasUninvitedPlayers = players.some(
		(player) => !gameNight.invitedPlayers.some((rsvp) => rsvp.playerId === player.id),
	);

	const handleStatusChange = async (rsvp: GameNightRsvps, state: GameNightRsvpState) => {
		if (rsvp.state === state) return;

		try {
			await onUpdateRsvp({
				id: rsvp.id,
				gameNightId: gameNight.id,
				playerId: rsvp.playerId,
				state,
			});
		} catch {
			return;
		}
	};

	const saveInvitedPlayers = async (playerIds: number[]) => {
		try {
			await onUpdateGameNight({
				id: gameNight.id,
				title: gameNight.title,
				notes: gameNight.notes,
				startDate: new Date(gameNight.startDate),
				hostId: gameNight.hostId,
				locationId: gameNight.locationId,
				suggestedGameIds: gameNight.suggestedGames.map((game) => game.id),
				invitedPlayerIds: playerIds.filter((id) => id !== gameNight.hostId),
			});
		} catch {
			return;
		}
	};

	const invitedIds = gameNight.invitedPlayers.map((rsvp) => rsvp.playerId);

	const handleRemove = (playerId: number) => saveInvitedPlayers(invitedIds.filter((id) => id !== playerId));

	const handleInvite = async (playerId: number) => {
		await saveInvitedPlayers([...invitedIds, playerId]);
		setQuery("");
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent className="md:max-w-xl! max-md:top-auto! max-md:bottom-0! max-md:w-full! max-md:max-w-none! max-md:translate-y-0! max-md:max-h-[90dvh]! max-md:rounded-b-none! max-md:p-4!">
				<div className="flex flex-col gap-0.5 pr-6">
					<BgtDialogTitle>{t("rsvp.title")}</BgtDialogTitle>
					<BgtDialogDescription className="text-sm">
						{t("rsvp.subtitle", {
							title: gameNight.title,
							date: toDisplayDateTime(
								gameNight.startDate,
								settings.dateFormat,
								settings.timeFormat,
								settings.uiLanguage,
							),
						})}
					</BgtDialogDescription>
				</div>

				<div className="flex flex-col gap-4">
					<div className="flex min-w-0 flex-col gap-3">
						<RsvpProgress rsvps={gameNight.invitedPlayers} />

						<ul className="flex flex-col gap-1.5 md:max-h-[45vh] md:overflow-y-auto">
							{rsvps.map((rsvp) => {
								const isHost = rsvp.playerId === gameNight.hostId;
								return (
									<li
										key={rsvp.id}
										data-testid={`rsvp-row-${rsvp.playerId}`}
										className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-x-2.5 gap-y-2 rounded-lg bg-white/5 px-2.5 py-2 md:grid-cols-[auto_minmax(0,1fr)_auto_auto]"
									>
										<div className="col-start-1 row-start-1">
											<BgtAvatar title={rsvp.player.name} image={rsvp.player.image} />
										</div>
										<div className="col-start-2 row-start-1 min-w-0">
											<BgtText size="3" weight="bold" className="wrap-anywhere">
												{rsvp.player.name}
											</BgtText>
											{isHost && (
												<BgtText size="1" color="gray">
													{t("rsvp.host")}
												</BgtText>
											)}
										</div>
										{isHost ? (
											<span className="col-start-3 row-start-1 justify-self-end rounded-full bg-green-500/20 px-2.5 py-0.5 text-xs font-semibold text-green-300 md:col-end-5">
												{t("rsvp.coming")}
											</span>
										) : (
											<>
												<fieldset
													aria-label={t("rsvp.player-status", { name: rsvp.player.name })}
													className="col-start-1 col-end-4 row-start-2 grid min-w-0 grid-cols-3 overflow-hidden rounded-lg border border-white/10 md:col-start-3 md:col-end-4 md:row-start-1 md:inline-flex"
												>
													{options.map((option) => {
														const pressed = rsvp.state === option.state;
														return (
															<button
																key={option.state}
																type="button"
																aria-pressed={pressed}
																disabled={isLoading}
																onClick={() => handleStatusChange(rsvp, option.state)}
																className={cx(
																	"px-2.5 py-1.5 text-xs font-semibold transition-colors not-first:border-l not-first:border-white/10 disabled:cursor-not-allowed disabled:opacity-60",
																	pressed ? option.active : "bg-input text-gray-400 hover:bg-white/10",
																)}
															>
																{t(option.label)}
															</button>
														);
													})}
												</fieldset>
												<BgtIconButton
													intent="subtile"
													className="col-start-3 row-start-1 md:col-start-4"
													icon={<Cross className="size-4" />}
													disabled={isLoading}
													aria-label={t("rsvp.remove", { name: rsvp.player.name })}
													onClick={() => handleRemove(rsvp.playerId)}
												/>
											</>
										)}
									</li>
								);
							})}
						</ul>
					</div>

					<div className="flex min-w-0 flex-col gap-4">
						{hasUninvitedPlayers && (
							<div className="flex flex-col gap-2">
								<BgtText size="2" weight="bold">
									{t("rsvp.invite-more")}
								</BgtText>
								<BgtSimpleInputField
									type="text"
									value={query}
									onChange={(event) => setQuery(event.target.value)}
									placeholder={t("rsvp.search-players")}
									ariaLabel={t("rsvp.invite-more")}
									className="w-full border border-primary/30 bg-primary/10"
									inputClassName="placeholder:text-primary/80"
									prefixLabel={<SearchIcon className="size-5 text-primary" />}
								/>
								{suggestions.length === 0 ? (
									<BgtText size="2" color="gray">
										{t("rsvp.no-players-found")}
									</BgtText>
								) : (
									<div className="flex flex-wrap gap-2">
										{suggestions.map((player) => (
											<button
												key={player.id}
												type="button"
												disabled={isLoading}
												onClick={() => handleInvite(player.id)}
												aria-label={t("rsvp.invite", { name: player.name })}
												className="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/5 py-1 pr-3 pl-1 text-xs font-medium hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-60"
											>
												<BgtAvatar title={player.name} image={player.image} size="small" />
												{player.name}
												<PlusIcon className="size-3.5 text-primary" />
											</button>
										))}
									</div>
								)}
							</div>
						)}

						<div className="flex flex-col gap-2">
							<BgtText size="2" weight="bold">
								{t("rsvp.invite-link")}
							</BgtText>
							<div className="flex min-w-0 items-center gap-2 rounded-lg border border-dashed border-white/15 bg-input py-1.5 pr-1.5 pl-3">
								<code className="min-w-0 flex-1 truncate text-xs text-gray-400">{link}</code>
								<BgtButton variant="cancel" size="1" onClick={copy} disabled={copied}>
									{copied ? t("card.copied") : t("rsvp.copy")}
								</BgtButton>
							</div>
						</div>
					</div>
				</div>

				<BgtDialogClose className="justify-end">
					<BgtButton variant="primary" onClick={close}>
						{t("common:done")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
