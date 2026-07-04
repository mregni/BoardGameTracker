import { useForm } from "@tanstack/react-form";
import { createFileRoute, useNavigate, useRouter } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Bars } from "react-loading-icons";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtImageSelector, BgtInputField } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { CreatePlayerSchema, type Player } from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { usePlayerModal } from "./-hooks/usePlayerModal";

export const Route = createFileRoute("/players/new")({
	component: RouteComponent,
});

function RouteComponent() {
	const { t } = useTranslation(["player", "common"]);
	const navigate = useNavigate();
	const router = useRouter();
	const [image, setImage] = useState<File | undefined | null>(undefined);

	const { savePlayer, uploadImage, isLoading } = usePlayerModal({});

	const form = useForm({
		defaultValues: {
			name: "",
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreatePlayerSchema.parse(value);

			const player: Player = {
				id: 0,
				name: validatedData.name,
				image: null,
				badges: [],
			};

			if (image !== undefined) {
				const savedImage = await uploadImage({ type: 0, file: image });
				player.image = savedImage ?? null;
			}

			const savedPlayer = await savePlayer(player);
			navigate({ to: `/players/${savedPlayer.id}` });
		},
	});

	const handleCancel = () => {
		router.history.back();
	};

	return (
		<BgtPage>
			<BgtPageContent className="flex-1 items-center">
				<BgtCard title={t("new.title")} className="w-full max-w-xl my-auto">
					<p className="text-cancel mb-4">{t("new.description")}</p>
					<form onSubmit={handleFormSubmit(form)} className="w-full">
						<div className="flex flex-row gap-3 mb-6">
							<div className="flex-none">
								<BgtImageSelector image={image} setImage={setImage} />
							</div>
							<div className="grow">
								<form.Field
									name="name"
									validators={{
										onChange: ({ value }) => {
											const result = CreatePlayerSchema.shape.name.safeParse(value);
											if (!result.success) {
												return t(result.error.issues[0].message);
											}
											return undefined;
										},
									}}
								>
									{(field) => (
										<BgtInputField
											field={field}
											type="text"
											placeholder={t("name.placeholder")}
											label={t("common:name")}
											disabled={isLoading}
										/>
									)}
								</form.Field>
							</div>
						</div>
						<div className="flex flex-row gap-2">
							<BgtButton variant="cancel" type="button" onClick={handleCancel} disabled={isLoading}>
								{t("common:cancel")}
							</BgtButton>
							<BgtButton type="submit" variant="primary" disabled={isLoading} className="flex-1">
								{isLoading && <Bars className="size-4" />}
								{t("new.save")}
							</BgtButton>
						</div>
					</form>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}
