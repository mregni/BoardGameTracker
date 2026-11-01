import { useTranslation } from "react-i18next";
import PencilIcon from "@/assets/icons/pencil.svg?react";
import TrashIcon from "@/assets/icons/trash.svg?react";
import { BgtIconButton } from "../BgtIconButton/BgtIconButton";

interface Props {
	name?: string;
	onDelete: () => void;
	onEdit: () => void;
}

export const BgtEditDeleteButtons = (props: Props) => {
	const { name, onDelete, onEdit } = props;
	const { t } = useTranslation("common");

	return (
		<div className="flex-row justify-end gap-2 flex">
			<BgtIconButton
				size="2"
				onClick={onEdit}
				icon={<PencilIcon />}
				aria-label={name ? t("edit-named", { name }) : t("edit")}
			/>
			<BgtIconButton
				size="2"
				intent="danger"
				onClick={onDelete}
				icon={<TrashIcon />}
				aria-label={name ? t("delete.named", { name }) : t("delete.button")}
			/>
		</div>
	);
};
