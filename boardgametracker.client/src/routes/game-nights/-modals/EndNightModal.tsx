import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import type { ModalProps } from "@/models";

interface Props extends ModalProps {
	isPending: boolean;
	onConfirm: () => Promise<unknown>;
}

export const EndNightModal = (props: Props) => {
	const { open, close, isPending, onConfirm } = props;
	const { t } = useTranslation(["game-nights", "common"]);

	const handleConfirm = async () => {
		await onConfirm();
		close();
	};

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("end.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("end.description")}</BgtDialogDescription>
				<BgtDialogClose>
					<BgtButton variant="cancel" onClick={close} disabled={isPending}>
						{t("common:cancel")}
					</BgtButton>
					<BgtButton variant="primary" onClick={handleConfirm} disabled={isPending}>
						{t("end.button")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
