import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { useAuth } from "@/hooks/useAuth";
import { useModalState } from "@/hooks/useModalState";
import { usePermissions } from "@/hooks/usePermissions";
import { useDangerZone } from "../-hooks/useDangerZone";
import { ResetConfirmModal } from "../-modals/ResetConfirmModal";
import { SettingsSection } from "./SettingsSection";

export const DangerZoneSection = () => {
	const { t } = useTranslation(["settings", "common"]);
	const { isAdmin } = usePermissions();
	const authEnabled = useAuth((s) => s.authStatus?.authEnabled ?? false);
	const resetModal = useModalState();
	const factoryModal = useModalState();
	const { resetDatabase, isResetting, factoryReset, isFactoryResetting } = useDangerZone();

	if (!authEnabled || !isAdmin) {
		return null;
	}

	return (
		<SettingsSection title={t("advanced.danger.title")} description={t("advanced.danger.description")}>
			<div className="flex flex-col gap-3 md:flex-row">
				<BgtButton variant="error" onClick={resetModal.show}>
					{t("advanced.danger.reset.button")}
				</BgtButton>
				<BgtButton variant="error" onClick={factoryModal.show}>
					{t("advanced.danger.factory-reset.button")}
				</BgtButton>
			</div>

			{resetModal.isOpen && (
				<ResetConfirmModal
					open={true}
					close={resetModal.hide}
					isLoading={isResetting}
					title={t("advanced.danger.reset.confirm.title")}
					description={t("advanced.danger.reset.confirm.description")}
					keyword={t("advanced.danger.reset.confirm.keyword")}
					inputLabel={t("advanced.danger.reset.confirm.input-label")}
					confirmLabel={t("advanced.danger.reset.confirm.button")}
					onConfirm={async () => {
						await resetDatabase();
						resetModal.hide();
					}}
				/>
			)}

			{factoryModal.isOpen && (
				<ResetConfirmModal
					open={true}
					close={factoryModal.hide}
					isLoading={isFactoryResetting}
					title={t("advanced.danger.factory-reset.confirm.title")}
					description={t("advanced.danger.factory-reset.confirm.description")}
					keyword={t("advanced.danger.factory-reset.confirm.keyword")}
					inputLabel={t("advanced.danger.factory-reset.confirm.input-label")}
					confirmLabel={t("advanced.danger.factory-reset.confirm.button")}
					onConfirm={async () => {
						await factoryReset();
						factoryModal.hide();
					}}
				/>
			)}
		</SettingsSection>
	);
};
