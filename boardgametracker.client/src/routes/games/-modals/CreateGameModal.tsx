import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import Database from "@/assets/icons/database.svg?react";
import Keyboard from "@/assets/icons/keyboard.svg?react";
import MagnifyingGlass from "@/assets/icons/magnifying-glass.svg?react";
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
	bggEnabled: boolean;
	openBgg: () => void;
	openManual: () => void;
}

const CreateGameModal = (props: Props) => {
	const { open, close, bggEnabled, openBgg, openManual } = props;
	const { t } = useTranslation(["game", "common"]);
	const navigate = useNavigate();

	const handleBggImport = useCallback(() => {
		navigate({ to: "/games/import/start" });
	}, [navigate]);

	return (
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("new.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("new.description")}</BgtDialogDescription>
				<div className="flex flex-col gap-4 mt-3 mb-3">
					{bggEnabled && <BgtBigButton title={t("new.bgg-title")} subText={t("new.bgg-subtext")} icon={MagnifyingGlass} onClick={openBgg} />}
					<BgtBigButton title={t("new.manual-title")} subText={t("new.manual-subtext")} icon={Keyboard} onClick={openManual} />
					{bggEnabled && (
						<BgtBigButton
							title={t("new.bgg-import-title")}
							subText={t("new.bgg-import-subtext")}
							icon={Database}
							onClick={handleBggImport}
						/>
					)}
				</div>
				<BgtDialogClose>
					<BgtButton variant="cancel" className="flex-1" onClick={close}>
						{t("common:cancel")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};

export default CreateGameModal;
