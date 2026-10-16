import { type AnyFieldApi, useSelector } from "@tanstack/react-form";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtDateTimePicker, BgtFieldLabel, BgtInputField, BgtSelect, BgtTextArea } from "@/components/BgtForm";
import { withForm } from "@/hooks/form";
import { CreateSessionSchema, type Game, type Location, type Player } from "@/models";
import { zodValidator } from "@/utils/zodValidator";
import type { SessionFormState } from "../-hooks/useSessionFormState";
import { toGameSelectItems } from "../-utils/gameSelectItems";
import { sessionFormOpts } from "../-utils/sessionFormOpts";
import { SessionExpansionSelector } from "./SessionExpansionSelector";
import { SessionPlayerManager } from "./SessionPlayerManager";

export const SessionFormFields = withForm({
	...sessionFormOpts,
	props: {
		games: [] as Game[],
		recentGameIds: [] as number[],
		recentGamesLabel: "",
		fixedGame: null as { title: string; image: string | null } | null,
		locations: [] as Location[],
		playerList: [] as Player[],
		frequentPlayerIds: [] as number[],
		state: {} as SessionFormState,
		disabled: false,
		onGameChange: (() => {}) as (gameId: number) => void,
		onMinutesChange: (() => {}) as (minutes: number | undefined) => void,
	},
	render: function Render({
		form,
		games,
		recentGameIds,
		recentGamesLabel,
		fixedGame,
		locations,
		playerList,
		frequentPlayerIds,
		state,
		disabled,
		onGameChange,
		onMinutesChange,
	}) {
		const { t } = useTranslation(["player-session", "common"]);

		const gameSelectItems = useMemo(
			() => toGameSelectItems(games, recentGameIds, recentGamesLabel, t("form.game.all")),
			[games, recentGameIds, recentGamesLabel, t],
		);

		const locationsSelectItems = useMemo(
			() => [
				{ value: 0, label: t("new.location.none") },
				...locations.map((x: Location) => ({ value: x.id, label: x.name })),
			],
			[locations, t],
		);

		const selectedGameId = useSelector(form.store, (formState) => formState.values.gameId);

		return (
			<div
				data-testid="session-form-fields"
				className="flex flex-col gap-4 lg:grid lg:grid-cols-2 lg:grid-rows-[auto_auto_1fr] lg:gap-x-6"
			>
				<div className="@container flex min-w-0 flex-col gap-3 lg:col-start-1 lg:row-start-1">
					{fixedGame ? (
						<div className="flex flex-col">
							<BgtFieldLabel>{t("new.game.label")}</BgtFieldLabel>
							<div className="flex h-11 items-center gap-2 md:h-10">
								<BgtAvatar size="medium" title={fixedGame.title} image={fixedGame.image} />
								<span className="font-semibold">{fixedGame.title}</span>
							</div>
						</div>
					) : (
						<form.Field
							name="gameId"
							validators={zodValidator(CreateSessionSchema, "gameId")}
							listeners={{ onChange: ({ value }) => onGameChange(value) }}
						>
							{(field: AnyFieldApi) => (
								<BgtSelect
									field={field}
									hasSearch
									items={gameSelectItems}
									label={t("new.game.label")}
									disabled={disabled}
									placeholder={t("new.game.placeholder")}
								/>
							)}
						</form.Field>
					)}
					<div className="grid grid-cols-2 gap-3 @2xl:grid-cols-[minmax(0,1.6fr)_minmax(0,0.7fr)_minmax(0,1fr)]">
						<div className="col-span-2 @2xl:col-span-1">
							<form.Field name="start" validators={zodValidator(CreateSessionSchema, "start")}>
								{(field: AnyFieldApi) => (
									<BgtDateTimePicker field={field} disabled={disabled} label={t("new.start.label")} />
								)}
							</form.Field>
						</div>
						<form.Field
							name="minutes"
							validators={zodValidator(CreateSessionSchema, "minutes")}
							listeners={{ onChange: ({ value }) => onMinutesChange(value) }}
						>
							{(field: AnyFieldApi) => (
								<BgtInputField
									field={field}
									type="number"
									inputMode="numeric"
									disabled={disabled}
									label={t("new.duration.label")}
									suffixLabel={t("common:minutes-abbreviation")}
								/>
							)}
						</form.Field>
						<form.Field name="locationId" validators={zodValidator(CreateSessionSchema, "locationId")}>
							{(field: AnyFieldApi) => (
								<BgtSelect
									field={field}
									items={locationsSelectItems}
									label={t("new.location.label")}
									disabled={disabled}
									placeholder={t("new.location.placeholder")}
								/>
							)}
						</form.Field>
					</div>
				</div>

				<div className="min-w-0 lg:col-start-2 lg:row-span-3 lg:row-start-1">
					<SessionPlayerManager
						form={form}
						playerList={playerList}
						frequentPlayerIds={frequentPlayerIds}
						hasScoring={state.hasScoring}
						disabled={disabled}
						onAddPlayer={state.addPlayer}
						onRemovePlayer={state.removePlayer}
						onScoreChange={state.setScore}
						onToggleWon={state.toggleWon}
						onToggleFirstPlay={state.toggleFirstPlay}
					/>
				</div>

				<div className="min-w-0 lg:col-start-1 lg:row-start-2">
					<SessionExpansionSelector
						expansionList={state.expansionList}
						selectedIds={state.selectedExpansionIds}
						selectedGameId={selectedGameId}
						disabled={disabled}
						onSelectionChange={state.setSelectedExpansionIds}
					/>
				</div>

				<div className="min-w-0 lg:col-start-1 lg:row-start-3">
					<form.Field name="comment" validators={zodValidator(CreateSessionSchema, "comment")}>
						{(field: AnyFieldApi) => (
							<BgtTextArea field={field} disabled={disabled} rows={2} label={t("new.comment.label")} />
						)}
					</form.Field>
				</div>
			</div>
		);
	},
});
