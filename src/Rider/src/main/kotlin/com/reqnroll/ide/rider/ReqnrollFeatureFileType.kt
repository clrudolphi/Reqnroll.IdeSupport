package com.reqnroll.ide.rider

import com.intellij.openapi.fileTypes.LanguageFileType
import javax.swing.Icon

object ReqnrollFeatureFileType : LanguageFileType(ReqnrollFeatureLanguage) {
    override fun getName() = "Reqnroll Feature"
    override fun getDescription() = "Reqnroll/Gherkin feature file"
    override fun getDefaultExtension() = "feature"
    override fun getIcon(): Icon? = null
}
