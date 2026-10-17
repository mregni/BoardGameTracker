import type { AnyFieldApi } from "@tanstack/react-form";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtSelect } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { withForm } from "@/hooks/form";
import { type CreateSessionPlayer, CreateSessionSchema, type Player } from "@/models";
import { CreatePlayerModal } from "@/routes/players/-modals/CreatePlayerModal";
import { zodValidator } from "@/utils/zodValidator";
import { sessionFormOpts } from "../-utils/sessionFormOpts";
import { splitSuggested } from "../-utils/splitSuggested";
import { SessionPlayerRow } from "./SessionPlayerRow";

const toPlayerOptions = (players: Player[], selected: CreateSessionPlayer[], frequentIds: number[]) => {
	const available = players.filter((x) => !selected.some((y) => y.playerId === x.id));
	const { suggested, rest } = splitSuggested(available, frequentIds);
	const others = [...rest];
	others.sort((a, b) => a.name.localeCompare(b.name));
	return [...suggested, ...others].map((x) => ({
		value: x.id,
		label: x.name,
		image: x.image,
	}));
};

export const SessionPlayerManager = withForm({
	...sessionFormOpts,
	props: {
		playerList: [] as Player[],
		frequentPlayerIds: [] as number[],
		hasScoring: true,
		isCoop: false,
		disabled: false,
		onAddPlayer: (() => {}) as (playerId: number) => void,
		onRemovePlayer: (() => {}) as (playerId: number) => void,
		onScoreChange: (() => {}) as (playerId: number, score: number | null) => void,
		onToggleWon: (() => {}) as (playerId: number) => void,
		onToggleFirstPlay: (() => {}) as (playerId: number) => void,
	},
	render: function Render({
		form,
		playerList,
		frequentPlayerIds,
		hasScoring,
		isCoop,
		disabled,
		onAddPlayer,
		onRemovePlayer,
		onScoreChange,
		onToggleWon,
		onToggleFirstPlay,
	}) {
		const { t } = useTranslation(["player-session", "player"]);
		const [isCreateOpen, setIsCreateOpen] = useState(false);

		return (
			<form.Field name="playerSessions" validators={zodValidator(CreateSessionSchema, "playerSessions")}>
				{(field: AnyFieldApi) => {
					const players: CreateSessionPlayer[] = field.state.value;
					const errors: string[] =
						field.state.meta.isTouched || form.state.submissionAttempts > 0 ? field.state.meta.errors : [];

					return (
						<div className="flex flex-col gap-2">
							<BgtSelect
								value={null}
								onValueChange={(value) => onAddPlayer(Number(value))}
								errors={errors}
								hasSearch
								showAvatars
								items={toPlayerOptions(playerList, players, frequentPlayerIds)}
								disabled={disabled}
								label={hasScoring && !isCoop ? t("form.players.label") : t("form.players.label-no-scoring")}
								placeholder={t("form.players.search")}
							/>
							<div>
								<BgtButton
									type="button"
									variant="text"
									size="1"
									disabled={disabled}
									onClick={() => setIsCreateOpen(true)}
								>
									+ {t("new.create-player")}
								</BgtButton>
							</div>
							{players.length === 0 && errors.length === 0 && (
								<BgtText size="2" color="white" opacity={50}>
									{t("player:new.players.none")}
								</BgtText>
							)}
							{players.length > 0 && (
								<ul className="@container flex flex-col gap-1.5">
									{players.map((entry) => (
										<SessionPlayerRow
											key={entry.playerId}
											entry={entry}
											player={playerList.find((x: Player) => x.id === entry.playerId) ?? null}
											hasScoring={hasScoring}
											isCoop={isCoop}
											disabled={disabled}
											onScoreChange={onScoreChange}
											onToggleWon={onToggleWon}
											onToggleFirstPlay={onToggleFirstPlay}
											onRemove={onRemovePlayer}
										/>
									))}
								</ul>
							)}
							<CreatePlayerModal
								open={isCreateOpen}
								close={() => setIsCreateOpen(false)}
								onPlayerCreated={(player: Player) => onAddPlayer(player.id)}
							/>
						</div>
					);
				}}
			</form.Field>
		);
	},
});
