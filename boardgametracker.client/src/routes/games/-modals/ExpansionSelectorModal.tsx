import { useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogClose,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtCheckboxList } from "@/components/BgtForm";
import type { ModalProps } from "@/models";
import { useExpansionSelectorModal } from "../-hooks/useExpansionSelectorModal";

interface Props extends ModalProps {
	gameId: number;
	selectedExpansions: number[];
}

export const ExpansionSelectorModal = (props: Props) => {
	const { open, close, gameId, selectedExpansions } = props;
	const { t } = useTranslation(["game", "common"]);
	const [selectedIds, setSelectedIds] = useState<number[]>(selectedExpansions);

	const { expansions, isLoading, isPending, saveExpansions } = useExpansionSelectorModal({ gameId });

	const saveModal = () => {
		saveExpansions({ gameId, expansionBggIds: selectedIds }).finally(() => {
			close();
		});
	};

	return (
		<BgtDialog open={open}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("expansions.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("expansions.description")}</BgtDialogDescription>
				<div className="my-4">
					{isLoading && <div>{t("common:loading-data")}</div>}
					<BgtCheckboxList
						items={expansions}
						selectedIds={selectedExpansions}
						onSelectionChange={(ids) => setSelectedIds(ids)}
						disabled={isLoading || isPending}
					/>
				</div>
				<BgtDialogClose>
					<BgtButton variant="cancel" onClick={() => close()} disabled={isLoading || isPending}>
						{t("common:cancel")}
					</BgtButton>
					<BgtButton type="button" variant="primary" disabled={isLoading} onClick={saveModal || isPending}>
						{t("expansions.update")}
					</BgtButton>
				</BgtDialogClose>
			</BgtDialogContent>
		</BgtDialog>
	);
};
