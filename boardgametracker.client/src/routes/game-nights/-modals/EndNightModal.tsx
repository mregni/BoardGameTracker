import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
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
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={t("end.button")}
					onConfirm={handleConfirm}
					loading={isPending}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
