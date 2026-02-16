import PencilIcon from "@/assets/icons/pencil.svg?react";

import TrashIcon from "@/assets/icons/trash.svg?react";
import { BgtIconButton } from "../BgtIconButton/BgtIconButton";

interface Props {
	onDelete: () => void;
	onEdit: () => void;
}

export const BgtEditDeleteButtons = (props: Props) => {
	const { onDelete, onEdit } = props;

	return (
		<div className="flex-row justify-end gap-2 flex">
			<BgtIconButton size="2" onClick={onEdit} icon={<PencilIcon />} />
			<BgtIconButton size="2" intent="danger" onClick={onDelete} icon={<TrashIcon />} />
		</div>
	);
};
