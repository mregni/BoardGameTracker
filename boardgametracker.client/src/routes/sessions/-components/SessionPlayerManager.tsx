import type { AnyFieldApi } from "@tanstack/react-form";
import { useEffect, useId, useRef } from "react";
import { useTranslation } from "react-i18next";
import { BgtFormErrors } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { withForm } from "@/hooks/form";
import { type CreateSessionPlayer, CreateSessionSchema, type Player } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import { sessionFormOpts } from "../-utils/sessionFormOpts";
import { SessionPlayerPicker } from "./SessionPlayerPicker";
import { SessionPlayerRow } from "./SessionPlayerRow";

export const SessionPlayerManager = withForm({
	...sessionFormOpts,
	props: {
		players: [] as CreateSessionPlayer[],
		playerList: [] as Player[],
		frequentPlayerIds: [] as number[],
		hasScoring: true,
		disabled: false,
		onAddPlayer: (() => {}) as (playerId: number) => void,
		onCreatePlayer: (async () => undefined) as (name: string) => Promise<Player | undefined>,
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
		onCreatePlayer,
		onRemovePlayer,
		onScoreChange,
		onToggleWon,
		onToggleFirstPlay,
	}) {
		const { t } = useTranslation(["player-session", "player"]);
		const inputId = useId();
		const isFirstSync = useRef(true);

		useEffect(() => {
			const silent = isFirstSync.current;
			isFirstSync.current = false;
			form.setFieldValue("playerSessions", players, { dontUpdateMeta: silent });
		}, [form, players]);

		return (
			<form.Field name="playerSessions" validators={zodValidator(CreateSessionSchema, "playerSessions")}>
				{(field: AnyFieldApi) => {
					const errors: string[] =
						field.state.meta.isTouched || form.state.submissionAttempts > 0 ? field.state.meta.errors : [];

					return (
						<div className="flex flex-col gap-2">
							<label htmlFor={inputId} className="text-[15px] font-medium leading-7">
								{hasScoring ? t("form.players.label") : t("form.players.label-no-scoring")}
							</label>
							<SessionPlayerPicker
								inputId={inputId}
								players={playerList}
								selectedPlayerIds={players.map((x: CreateSessionPlayer) => x.playerId)}
								frequentPlayerIds={frequentPlayerIds}
								disabled={disabled}
								hasErrors={errors.length > 0}
								onAddPlayer={onAddPlayer}
								onCreatePlayer={onCreatePlayer}
							/>
							<BgtFormErrors errors={errors} />
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
						</div>
					);
				}}
			</form.Field>
		);
	},
});
