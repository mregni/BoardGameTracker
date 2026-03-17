import type { AnyFieldApi } from "@tanstack/react-form";
import { useRouter } from "@tanstack/react-router";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCenteredCard } from "@/components/BgtCard/BgtCenteredCard";
import { BgtImageSelector, BgtSwitch } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { useAppForm } from "@/hooks/form";
import type { Game } from "@/models";
import { type CreateGame, CreateGameSchema } from "@/models/Games/CreateGame";
import { toInputDate } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useGameForm } from "../-hooks/useGameForm";
import { useImageUpload } from "../-hooks/useImageUpload";
import { gameFormOpts } from "../-utils/gameFormOpts";
import { GameFormBasicFields } from "./GameFormBasicFields";
import { GameFormPlayerFields } from "./GameFormPlayerFields";
import { GameFormTimeFields } from "./GameFormTimeFields";

interface Props {
	onClick: (data: CreateGame) => Promise<void>;
	buttonText: string;
	disabled: boolean;
	game?: Game;
	title: string;
}

export const GameForm = (props: Props) => {
	const { onClick, buttonText, disabled, game, title } = props;
	const { settings } = useGameForm();
	const { t } = useTranslation(["game", "common"]);
	const router = useRouter();
	const { poster, setPoster, uploadPoster } = useImageUpload(game?.image);

	const form = useAppForm({
		...gameFormOpts,
		defaultValues: {
			id: game?.id ?? undefined,
			title: game?.title ?? "",
			hasScoring: game?.hasScoring ?? true,
			description: game?.description ?? "",
			state: game?.state ?? 0,
			yearPublished: game?.yearPublished ?? undefined,
			maxPlayers: game?.maxPlayers ?? undefined,
			minPlayers: game?.minPlayers ?? undefined,
			maxPlayTime: game?.maxPlayTime ?? undefined,
			minPlayTime: game?.minPlayTime ?? undefined,
			minAge: game?.minAge ?? undefined,
			bggId: game?.bggId ?? undefined,
			buyingPrice: game?.buyingPrice ?? 0,
			additionDate: toInputDate(game?.additionDate ?? undefined, true),
			image: game?.image ?? null,
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreateGameSchema.parse(value) as CreateGame;
			validatedData.image = await uploadPoster(poster);
			await onClick(validatedData);
		},
	});

	const handleCancel = useCallback(() => {
		router.history.back();
	}, [router]);

	return (
		<BgtPage>
			<BgtPageContent centered>
				<BgtCenteredCard title={title}>
					<form onSubmit={handleFormSubmit(form)} className="w-full">
						<div className="flex flex-col gap-3 w-full">
							<div className="flex flex-row gap-3">
								<div className="flex-none">
									<BgtImageSelector image={poster} setImage={setPoster} defaultImage={game?.image} />
								</div>
								<div className="grow">
									<GameFormBasicFields form={form} disabled={disabled} />
								</div>
							</div>

							<GameFormPlayerFields form={form} disabled={disabled} currency={settings?.currency} />

							<GameFormTimeFields form={form} disabled={disabled} />

							{game === undefined && (
								<form.Field name="hasScoring" validators={zodValidator(CreateGameSchema, "hasScoring")}>
									{(field: AnyFieldApi) => (
										<BgtSwitch field={field} label={t("scoring.label")} className="pt-3" disabled={disabled} />
									)}
								</form.Field>
							)}

							<div className="flex flex-row gap-2 mt-2">
								<BgtButton
									variant="cancel"
									type="button"
									className="flex-none"
									onClick={handleCancel}
									disabled={disabled}
								>
									{t("common:cancel")}
								</BgtButton>
								<BgtButton type="submit" className="flex-1" variant="primary" disabled={disabled}>
									{buttonText}
								</BgtButton>
							</div>
						</div>
					</form>
				</BgtCenteredCard>
			</BgtPageContent>
		</BgtPage>
	);
};
