import { useState } from "react";
import { useTranslation } from "react-i18next";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
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
		<BgtDialog open={open} onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("expansions.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("expansions.description")}</BgtDialogDescription>
				<div className="my-4">
					{isLoading && <div>{t("common:loading-data")}</div>}
					<BgtCheckboxList
						items={expansions}
						selectedIds={selectedIds}
						onSelectionChange={(ids) => setSelectedIds(ids)}
						disabled={isLoading || isPending}
					/>
				</div>
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={t("expansions.update")}
					onConfirm={saveModal}
					disabled={isLoading || isPending}
					cancelDisabled={isLoading || isPending}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
