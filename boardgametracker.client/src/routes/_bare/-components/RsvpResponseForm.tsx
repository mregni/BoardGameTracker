import { cx } from "class-variance-authority";
import { useId, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import Check from "@/assets/icons/check.svg?react";
import CircleHelp from "@/assets/icons/circle-help.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtSelect } from "@/components/BgtForm";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { type BgtSelectImageItem, GameNightRsvpState, type GameNightRsvps } from "@/models";
import type { RsvpAnswer } from "../-hooks/useRsvpData";

interface Props {
	invitedPlayers: GameNightRsvps[];
	hostId: number;
	initialAnswer: Pick<RsvpAnswer, "playerId" | "state"> | null;
	onSubmit: (rsvp: GameNightRsvps, state: GameNightRsvpState) => void;
	isSubmitting: boolean;
}

const responseOptions = [
	{ state: GameNightRsvpState.Accepted, label: "coming", Icon: Check, active: "bg-green-500/20 text-green-300" },
	{ state: GameNightRsvpState.Maybe, label: "maybe", Icon: CircleHelp, active: "bg-amber-500/20 text-amber-300" },
	{ state: GameNightRsvpState.Declined, label: "cant-come", Icon: XIcon, active: "bg-red-500/20 text-red-300" },
] as const;

const MAX_NAME_TILES = 8;

export const RsvpResponseForm = (props: Props) => {
	const { invitedPlayers, hostId, initialAnswer, onSubmit, isSubmitting } = props;
	const { t } = useTranslation("rsvp");
	const headingId = useId();
	const [playerId, setPlayerId] = useState<number | null>(initialAnswer?.playerId ?? null);
	const [state, setState] = useState<GameNightRsvpState | null>(initialAnswer?.state ?? null);

	const playerItems: BgtSelectImageItem[] = useMemo(
		() =>
			invitedPlayers
				.filter((rsvp) => rsvp.playerId !== hostId)
				.map((rsvp) => ({ value: rsvp.playerId, label: rsvp.player.name, image: rsvp.player.image })),
		[invitedPlayers, hostId],
	);

	const rsvp = invitedPlayers.find((x) => x.playerId === playerId);
	const canSubmit = !!rsvp && state !== null && !isSubmitting;

	const handleSubmit = () => {
		if (rsvp && state !== null) {
			onSubmit(rsvp, state);
		}
	};

	return (
		<BgtCard className="gap-4">
			<BgtHeading as="h2" size="4" id={headingId}>
				{t("can-you-make-it")}
			</BgtHeading>

			{playerItems.length > MAX_NAME_TILES ? (
				<BgtSelect
					label={t("who-are-you")}
					items={playerItems}
					placeholder={t("select-your-name")}
					hasSearch
					showAvatars
					value={playerId}
					onValueChange={(value) => setPlayerId(Number(value))}
				/>
			) : (
				<fieldset className="flex min-w-0 flex-col gap-2">
					<legend className="mb-2 text-sm font-semibold">{t("who-are-you")}</legend>
					<div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
						{playerItems.map((item) => {
							const pressed = playerId === item.value;
							return (
								<button
									key={item.value}
									type="button"
									aria-pressed={pressed}
									onClick={() => setPlayerId(Number(item.value))}
									className={cx(
										"flex min-h-12 min-w-0 items-center gap-2 rounded-lg border px-2.5 py-2 text-left text-sm font-medium transition-colors",
										pressed
											? "border-primary bg-primary/20 text-white"
											: "border-white/10 bg-input text-gray-300 hover:bg-white/10",
									)}
								>
									<BgtAvatar title={item.label} image={item.image} size="small" shape="round" />
									<span className="wrap-anywhere">{item.label}</span>
								</button>
							);
						})}
					</div>
				</fieldset>
			)}

			<fieldset
				aria-labelledby={headingId}
				className="grid min-w-0 grid-cols-3 overflow-hidden rounded-lg border border-white/10"
			>
				{responseOptions.map((option) => {
					const pressed = state === option.state;
					return (
						<button
							key={option.state}
							type="button"
							aria-pressed={pressed}
							onClick={() => setState(option.state)}
							className={cx(
								"flex min-h-14 flex-col items-center justify-center gap-1 px-2 py-2 text-sm font-semibold transition-colors not-first:border-l not-first:border-white/10",
								pressed ? option.active : "bg-input text-gray-400 hover:bg-white/10",
							)}
						>
							<option.Icon className="size-5" aria-hidden />
							{t(option.label)}
						</button>
					);
				})}
			</fieldset>

			<BgtButton onClick={handleSubmit} disabled={!canSubmit} variant="primary" size="3" className="w-full">
				{t("send-answer")}
			</BgtButton>
		</BgtCard>
	);
};
