import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "../../components/BgtDialog";

interface Props {
	open: boolean;
	close: () => void;
	onDelete: (() => Promise<void>) | (() => void);
	title: string;
	heading?: string;
	description: string;
	confirmLabel?: string;
}

export const BgtDeleteModal = (props: Props) => {
	const { open, close, onDelete, title, heading, description, confirmLabel } = props;
	const { t } = useTranslation();

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{heading ?? t("delete.title", { title: title })}</BgtDialogTitle>
				<BgtDialogDescription>{description}</BgtDialogDescription>
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={confirmLabel ?? t("delete.button")}
					onConfirm={onDelete}
					variant="destructive"
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
