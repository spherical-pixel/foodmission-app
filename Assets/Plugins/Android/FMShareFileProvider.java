package eu.foodmission.platform;

import androidx.core.content.FileProvider;

/**
 * FileProvider for the native share sheet (authority "<package>.fmshare", see FMShare.androidlib).
 * Its own subclass so another plugin declaring the stock androidx FileProvider cannot clash in the manifest merge.
 */
public class FMShareFileProvider extends FileProvider {
}
