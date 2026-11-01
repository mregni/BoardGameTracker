import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { BgtActionMenu, type BgtActionMenuItem } from "@/components/BgtActionMenu/BgtActionMenu";
import type { GameNight } from "@/models";
import { useInviteLink } from "../-hooks/useInviteLink";

interface Props {
	gameNight: GameNight;
	invitable: boolean;
	copyInMenu?: boolean;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
}

export const GameNightActions = (props: Props) => {
	const { gameNight, invitable, copyInMenu = true, onEdit, onDelete } = props;
	const { t } = useTranslation("game-nights");
	const { copy } = useInviteLink(gameNight);

	const items: BgtActionMenuItem[] = [
		...(invitable && copyInMenu
			? [
					{
						label: t("card.copy-link"),
						onSelect: () => {
							void copy().then((copied) => copied && toast.success(t("card.copied")));
						},
					},
				]
			: []),
		{ label: t("common:edit"), onSelect: () => onEdit(gameNight) },
		{ label: t("common:delete.button"), onSelect: () => onDelete(gameNight), destructive: true },
	];

	return <BgtActionMenu label={t("card.actions", { title: gameNight.title })} items={items} />;
};
