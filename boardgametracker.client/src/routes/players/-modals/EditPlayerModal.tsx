import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtFieldLabel, BgtImageSelector, BgtInputField, BgtSelect } from "@/components/BgtForm";
import { useAppForm } from "@/hooks/form";
import {
	type BgtSelectImageItem,
	type BgtSelectItem,
	CreatePlayerSchema,
	type ModalProps,
	type Player,
	type PlayerLinkedAccount,
	type UserDto,
} from "@/models";
import { handleFormSubmit } from "@/utils/formUtils";
import { zodValidator } from "@/utils/zodValidator";
import { useLinkedAccountOptions } from "../-hooks/useLinkedAccountOptions";
import { usePlayerModal } from "../-hooks/usePlayerModal";

interface Props extends ModalProps {
	player: Player;
}

const NOT_LINKED = "none";

const accountLabel = (account: { username: string; displayName?: string | null }) =>
	account.displayName && account.displayName !== account.username
		? `${account.username} (${account.displayName})`
		: account.username;

const LinkedAccountView = ({ account }: { account: PlayerLinkedAccount }) => {
	const { t } = useTranslation("player");

	return (
		<div className="flex flex-col">
			<BgtFieldLabel>{t("account.label")}</BgtFieldLabel>
			<div className="flex items-center gap-2" data-testid="linked-account">
				<BgtAvatar image={account.profileImage} title={account.username} size="medium" />
				<span>{accountLabel(account)}</span>
			</div>
		</div>
	);
};

const pictureHint = (player: Player, image: File | undefined | null) => {
	if (!player.linkedAccount || image !== undefined) {
		return null;
	}

	if (player.imageSource === "account") {
		return "account.uses-picture";
	}

	if (player.imageSource === "upload" && player.linkedAccount.profileImage) {
		return "account.own-picture";
	}

	return null;
};

export const EditPlayerModal = (props: Props) => {
	const { open, close, player } = props;
	const { t } = useTranslation(["player", "common"]);
	const [image, setImage] = useState<File | undefined | null>(undefined);

	const { updatePlayer, uploadImage, linkAccount, isLoading } = usePlayerModal({});
	const { authEnabled, canManage, users, players } = useLinkedAccountOptions();
	const currentAccountId = player.linkedAccount?.id ?? NOT_LINKED;

	const accountItems: (BgtSelectImageItem | BgtSelectItem)[] = useMemo(
		() => [
			{ value: NOT_LINKED, label: t("account.none") },
			...users.map((user: UserDto) => {
				const otherPlayer =
					user.playerId != null && user.playerId !== player.id
						? players.find((p) => p.id === user.playerId)
						: undefined;
				return {
					value: user.id,
					label: accountLabel(user),
					image: user.profileImage ?? null,
					hint: otherPlayer ? t("account.linked-to", { name: otherPlayer.name }) : undefined,
				};
			}),
		],
		[users, players, player.id, t],
	);

	const form = useAppForm({
		defaultValues: {
			name: player.name,
			email: player.email ?? "",
			linkedUserId: currentAccountId,
		},
		onSubmit: async ({ value }) => {
			const validatedData = CreatePlayerSchema.parse(value);

			const updatedPlayer: Player = {
				...player,
				name: validatedData.name,
				email: validatedData.email || null,
			};

			if (image !== undefined && image !== null) {
				const savedImage = await uploadImage({ type: 0, file: image });
				updatedPlayer.image = savedImage ?? null;
			} else if (image === null) {
				updatedPlayer.image = null;
			}

			await updatePlayer(updatedPlayer);

			if (canManage && value.linkedUserId !== currentAccountId) {
				await linkAccount({
					playerId: player.id,
					userId: value.linkedUserId === NOT_LINKED ? null : value.linkedUserId,
				});
			}

			close();
		},
	});

	const hint = pictureHint(player, image);

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent aria-describedby={undefined}>
				<BgtDialogTitle>{t("update.title")}</BgtDialogTitle>
				<form onSubmit={handleFormSubmit(form)}>
					<div className={hint ? "flex flex-row gap-3 mt-3" : "flex flex-row gap-3 mt-3 mb-6"}>
						<div className="flex-none">
							<BgtImageSelector image={image} setImage={setImage} defaultImage={player.image} />
						</div>
						<div className="grow">
							<form.Field name="name" validators={zodValidator(CreatePlayerSchema, "name")}>
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
							<form.Field name="email" validators={zodValidator(CreatePlayerSchema, "email")}>
								{(field) => (
									<BgtInputField
										field={field}
										type="text"
										placeholder={t("email.placeholder")}
										label={t("email.label")}
										disabled={isLoading}
									/>
								)}
							</form.Field>
							{canManage && (
								<form.Field name="linkedUserId">
									{(field) => {
										const selected = users.find((user) => user.id === field.state.value);
										const otherPlayerId =
											selected?.playerId != null && selected.playerId !== player.id ? selected.playerId : null;
										const otherPlayer = players.find((p) => p.id === otherPlayerId);
										return (
											<div className="flex flex-col gap-1">
												<BgtSelect
													field={field}
													label={t("account.label")}
													items={accountItems}
													hasSearch
													showAvatars
													disabled={isLoading}
												/>
												{otherPlayerId !== null && (
													<span className="text-xs text-white/60" data-testid="linked-account-move">
														{t("account.moves", { player: otherPlayer?.name ?? `#${otherPlayerId}` })}
													</span>
												)}
											</div>
										);
									}}
								</form.Field>
							)}
							{authEnabled && !canManage && player.linkedAccount && (
								<LinkedAccountView account={player.linkedAccount} />
							)}
						</div>
					</div>
					{hint && (
						<p className="text-xs text-white/60 mt-2 mb-6" data-testid="player-picture-hint">
							{t(hint, { username: player.linkedAccount?.username })}
						</p>
					)}
					<BgtDialogFooter
						onCancel={close}
						confirmLabel={t("update.save")}
						confirmType="submit"
						disabled={isLoading}
						cancelDisabled={isLoading}
					/>
				</form>
			</BgtDialogContent>
		</BgtDialog>
	);
};
