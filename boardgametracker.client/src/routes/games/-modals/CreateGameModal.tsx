import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import BgtBigButton from "@/components/BgtButton/BgtBigButton";
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
	openBgg: () => void;
	openManual: () => void;
}

const CreateGameModal = (props: Props) => {
	const { open, close, openBgg, openManual } = props;
	const { t } = useTranslation();
	const navigate = useNavigate();

	const handleBggImport = useCallback(() => {
		navigate({ to: "/games/import/start" });
	}, [navigate]);

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("game.new.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("game.new.description")}</BgtDialogDescription>
				<div className="flex flex-col gap-4 mt-3 mb-3">
					<BgtBigButton title={t("game.new.bgg-title")} subText={t("game.new.bgg-subtext")} onClick={openBgg} />
					<BgtBigButton
						title={t("game.new.manual-title")}
						subText={t("game.new.manual-subtext")}
						onClick={openManual}
					/>
					<BgtBigButton
						title={t("game.new.bgg-import-title")}
						subText={t("game.new.bgg-import-subtext")}
						onClick={handleBggImport}
					/>
				</div>
				<BgtDialogClose>
					<BgtButton variant="cancel" className="flex-1" onClick={close}>
						{t("common.cancel")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};

export default CreateGameModal;
