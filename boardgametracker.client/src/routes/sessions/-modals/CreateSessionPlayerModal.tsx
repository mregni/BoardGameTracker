import type { AnyFieldApi } from "@tanstack/react-form";
import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtInputField, BgtSelect, BgtSwitch } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import {
	type CreatePlayerSessionNoScoring,
	CreatePlayerSessionNoScoringSchema,
	CreatePlayerSessionSchema,
	type CreateSessionPlayer,
	type Player,
} from "@/models";
import { CreatePlayerModal } from "@/routes/players/-modals/CreatePlayerModal";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";

interface Props {
	open: boolean;
	hasScoring: boolean;
	onClose: (player: CreateSessionPlayer | CreatePlayerSessionNoScoring) => void;
	onCancel: () => void;
	selectedPlayerIds: number[];
	players: Player[];
}

const CreateSessionPlayerForm = (props: Props) => {
	const { open, hasScoring, onClose, selectedPlayerIds, onCancel, players } = props;
	const { t } = useTranslation();

	const [openCreatePlayerModal, setOpenCreatePlayerModal] = useState(false);
	const newlyCreatedPlayerIdRef = useRef<number | null>(null);

	const schema = hasScoring ? CreatePlayerSessionSchema : CreatePlayerSessionNoScoringSchema;

	const form = useAppForm({
		defaultValues: {
			playerId: "",
			firstPlay: false,
			won: false,
			score: 0,
		},
		onSubmit: async ({ value }) => {
			const validatedData = schema.parse(value);
			onClose(validatedData);
		},
	});

	const handlePlayerCreated = (player: Player) => {
		newlyCreatedPlayerIdRef.current = player.id;
	};

	useEffect(() => {
		if (newlyCreatedPlayerIdRef.current !== null) {
			const playerExists = players.some((p) => p.id === newlyCreatedPlayerIdRef.current);
			if (playerExists) {
				form.setFieldValue("playerId", String(newlyCreatedPlayerIdRef.current));
				newlyCreatedPlayerIdRef.current = null;
			}
		}
	}, [players, form]);

	return (
		<BgtDialog open={open}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("player-session.new.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("player-session.new.description")}</BgtDialogDescription>
				<form onSubmit={handleFormSubmit(form)}>
					<div className="flex flex-col gap-4 mt-3 mb-6">
						<form.Field name="playerId" validators={zodValidator(schema, "playerId")}>
							{(field: AnyFieldApi) => (
								<BgtSelect
									field={field}
									label={t("player-session.new.player.label")}
									items={players
										.filter((player) => !selectedPlayerIds.includes(player.id))
										.map((value) => ({
											label: value.name,
											value: value.id,
											image: value.image,
										}))}
								/>
							)}
						</form.Field>
						<div>
							<BgtButton type="button" variant="text" size="1" onClick={() => setOpenCreatePlayerModal(true)}>
								+ {t("player-session.new.create-player")}
							</BgtButton>
						</div>
						{hasScoring && (
							<form.Field name="score" validators={zodValidator(CreatePlayerSessionSchema, "score")}>
								{(field: AnyFieldApi) => (
									<BgtInputField field={field} type="number" label={t("player-session.score.label")} />
								)}
							</form.Field>
						)}
						<form.Field name="won" validators={zodValidator(schema, "won")}>
							{(field: AnyFieldApi) => (
								<BgtSwitch field={field} label={t("player-session.won.label")} className="mt-2" />
							)}
						</form.Field>
						<form.Field name="firstPlay" validators={zodValidator(schema, "firstPlay")}>
							{(field: AnyFieldApi) => (
								<BgtSwitch field={field} label={t("player-session.first-play.label")} className="mt-2" />
							)}
						</form.Field>
					</div>
					<BgtDialogClose>
						<BgtButton type="button" variant="cancel" onClick={() => onCancel()}>
							{t("common.cancel")}
						</BgtButton>
						<BgtButton type="submit" variant="primary">
							{t("player-session.new.save")}
						</BgtButton>
					</BgtDialogClose>
				</form>
			</BgtDialogContent>

			<CreatePlayerModal
				open={openCreatePlayerModal}
				close={() => setOpenCreatePlayerModal(false)}
				onPlayerCreated={handlePlayerCreated}
			/>
		</BgtDialog>
	);
};

export const CreateSessionPlayerModal = (props: Props) => {
	return props.open && <CreateSessionPlayerForm {...props} />;
};
