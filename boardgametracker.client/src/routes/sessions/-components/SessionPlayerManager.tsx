import type { AnyFieldApi } from "@tanstack/react-form";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtSelect } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { withForm } from "@/hooks/form";
import { type BgtSelectImageItem, type CreateSessionPlayer, CreateSessionSchema, type Player } from "@/models";
import { CreatePlayerModal } from "@/routes/players/-modals/CreatePlayerModal";
import { zodValidator } from "@/utils/zodValidator";
import { sessionFormOpts } from "../-utils/sessionFormOpts";
import { SessionPlayerRow } from "./SessionPlayerRow";

const toPlayerOptions = (players: Player[], selectedIds: number[], frequentIds: number[]): BgtSelectImageItem[] => {
	const available = players.filter((x) => !selectedIds.includes(x.id));
	const frequent = frequentIds
		.map((id) => available.find((x) => x.id === id))
		.filter((x): x is Player => x !== undefined);
	const rest = available.filter((x) => !frequent.includes(x)).sort((a, b) => a.name.localeCompare(b.name));

	return [...frequent, ...rest].map((x) => ({ value: x.id, label: x.name, image: x.image }));
};

export const SessionPlayerManager = withForm({
	...sessionFormOpts,
	props: {
		players: [] as CreateSessionPlayer[],
		playerList: [] as Player[],
		frequentPlayerIds: [] as number[],
		hasScoring: true,
		disabled: false,
		onAddPlayer: (() => {}) as (playerId: number) => void,
		onRemovePlayer: (() => {}) as (playerId: number) => void,
		onScoreChange: (() => {}) as (playerId: number, score: number | null) => void,
		onToggleWon: (() => {}) as (playerId: number) => void,
		onToggleFirstPlay: (() => {}) as (playerId: number) => void,
	},
	render: function Render({
		form,
		players,
		playerList,
		frequentPlayerIds,
		hasScoring,
		disabled,
		onAddPlayer,
		onRemovePlayer,
		onScoreChange,
		onToggleWon,
		onToggleFirstPlay,
	}) {
		const { t } = useTranslation(["player-session", "player"]);
		const [isCreateOpen, setIsCreateOpen] = useState(false);

		useEffect(() => {
			const quiet = players.length === 0 && !form.getFieldMeta("playerSessions")?.isTouched;
			form.setFieldValue("playerSessions", players, { dontUpdateMeta: quiet, dontValidate: quiet });
		}, [form, players]);

		const options = toPlayerOptions(
			playerList,
			players.map((x: CreateSessionPlayer) => x.playerId),
			frequentPlayerIds,
		);

		return (
			<form.Field name="playerSessions" validators={zodValidator(CreateSessionSchema, "playerSessions")}>
				{(field: AnyFieldApi) => {
					const errors: string[] =
						field.state.meta.isTouched || form.state.submissionAttempts > 0 ? field.state.meta.errors : [];

					const pickerField = {
						name: "playerPicker",
						state: { value: "", meta: { errors } },
						handleChange: (value: number | string) => onAddPlayer(Number(value)),
						handleBlur: () => {},
					} as unknown as AnyFieldApi;

					return (
						<div className="flex flex-col gap-2">
							<BgtSelect
								field={pickerField}
								hasSearch
								showAvatars
								items={options}
								disabled={disabled}
								label={hasScoring ? t("form.players.label") : t("form.players.label-no-scoring")}
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
								<BgtText color="primary">{t("player:new.players.none")}</BgtText>
							)}
							{players.length > 0 && (
								<ul className="@container flex flex-col gap-2">
									{players.map((entry: CreateSessionPlayer) => (
										<SessionPlayerRow
											key={entry.playerId}
											entry={entry}
											player={playerList.find((x: Player) => x.id === entry.playerId) ?? null}
											hasScoring={hasScoring}
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
