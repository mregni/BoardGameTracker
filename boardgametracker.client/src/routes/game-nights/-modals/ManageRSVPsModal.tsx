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
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtSimpleInputField } from "@/components/BgtForm/BgtSimpleInputField";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";
import { AvatarChipButton } from "@/components/GameNight/AvatarChip";
import { RsvpProgress } from "@/components/GameNight/RsvpProgress";
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
import { byName, sortRsvps } from "@/utils/sortRsvps";
import { useInviteLink } from "../-hooks/useInviteLink";
import { toUpdateGameNight } from "../-utils/toUpdateGameNight";

interface Props extends ModalProps {
	gameNight: GameNight | null;
	players: Player[];
	settings: Settings;
	onUpdateRsvp: (rsvp: UpdateGameNightRsvp) => void;
	onUpdateGameNight: (gameNight: UpdateGameNight) => void;
	isLoading: boolean;
}

const MAX_SUGGESTIONS = 8;

const options = [
	{ state: GameNightRsvpState.Accepted, label: "rsvp.coming", active: "bg-green-500/20 text-green-300" },
	{ state: GameNightRsvpState.Maybe, label: "rsvp.maybe", active: "bg-amber-500/20 text-amber-300" },
	{ state: GameNightRsvpState.Pending, label: "rsvp.no-answer", active: "bg-white/15 text-gray-200" },
	{ state: GameNightRsvpState.Declined, label: "rsvp.not-coming", active: "bg-red-500/20 text-red-300" },
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
	const [confirmingRemove, setConfirmingRemove] = useState<number | null>(null);

	const rsvps = useMemo(
		() => sortRsvps(gameNight.invitedPlayers, gameNight.hostId, byName),
		[gameNight.invitedPlayers, gameNight.hostId],
	);

	const uninvited = useMemo(
		() => players.filter((player) => !gameNight.invitedPlayers.some((rsvp) => rsvp.playerId === player.id)),
		[players, gameNight.invitedPlayers],
	);

	const search = query.trim().toLowerCase();
	const suggestions = uninvited
		.filter((player) => player.name.toLowerCase().includes(search))
		.slice(0, MAX_SUGGESTIONS);

	const handleStatusChange = (rsvp: GameNightRsvps, state: GameNightRsvpState) => {
		if (rsvp.state === state) return;
		onUpdateRsvp({ id: rsvp.id, gameNightId: gameNight.id, playerId: rsvp.playerId, state });
	};

	const saveInvitedPlayers = (playerIds: number[]) =>
		onUpdateGameNight({ ...toUpdateGameNight(gameNight), invitedPlayerIds: playerIds });

	const invitedIds = toUpdateGameNight(gameNight).invitedPlayerIds;

	const handleRemove = (playerId: number) => saveInvitedPlayers(invitedIds.filter((id) => id !== playerId));

	const handleInvite = (playerId: number) => {
		saveInvitedPlayers([...invitedIds, playerId]);
		setQuery("");
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent size="sheet" className="lg:max-w-5xl">
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

				<div
					className="flex flex-col gap-4 lg:grid lg:grid-cols-[minmax(0,3fr)_minmax(0,2fr)] lg:gap-6"
					data-testid="rsvp-columns"
				>
					<div className="flex min-w-0 flex-col gap-3">
						<RsvpProgress rsvps={gameNight.invitedPlayers} />

						<ul className="flex flex-col gap-1.5 md:max-h-[45vh] md:overflow-y-auto lg:max-h-[60vh]">
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
													className="col-start-1 col-end-4 row-start-2 grid min-w-0 grid-cols-4 overflow-hidden rounded-lg border border-white/10 md:col-start-3 md:col-end-4 md:row-start-1 md:inline-flex"
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
												{confirmingRemove === rsvp.playerId ? (
													<BgtButton
														variant="error"
														size="1"
														autoFocus
														className="col-start-3 row-start-1 md:col-start-4"
														disabled={isLoading}
														aria-label={t("rsvp.remove", { name: rsvp.player.name })}
														onBlur={() => setConfirmingRemove(null)}
														onClick={() => handleRemove(rsvp.playerId)}
													>
														{t("rsvp.confirm-remove")}
													</BgtButton>
												) : (
													<BgtIconButton
														intent="subtile"
														className="col-start-3 row-start-1 md:col-start-4"
														icon={<Cross className="size-4" />}
														disabled={isLoading}
														aria-label={t("rsvp.remove", { name: rsvp.player.name })}
														onClick={() => setConfirmingRemove(rsvp.playerId)}
													/>
												)}
											</>
										)}
									</li>
								);
							})}
						</ul>
					</div>

					<div className="flex min-w-0 flex-col gap-4">
						{uninvited.length > 0 && (
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
									inputClassName="placeholder:text-primary-text"
									prefixLabel={<SearchIcon className="size-5 text-primary" />}
								/>
								{suggestions.length === 0 ? (
									<BgtText size="2" color="gray">
										{t("rsvp.no-players-found")}
									</BgtText>
								) : (
									<div className="flex flex-wrap gap-2">
										{suggestions.map((player) => (
											<AvatarChipButton
												key={player.id}
												title={player.name}
												image={player.image}
												disabled={isLoading}
												onClick={() => handleInvite(player.id)}
												label={t("rsvp.invite", { name: player.name })}
											>
												<PlusIcon className="size-3.5 text-primary" />
											</AvatarChipButton>
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

				<BgtDialogFooter confirmLabel={t("common:close")} onConfirm={close} />
			</BgtDialogContent>
		</BgtDialog>
	);
};
